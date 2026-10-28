package ir.torsepan.mobile;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.security.KeyStore;
import java.util.UUID;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Only the push enrollment credential is stored locally. No workshop database or queued work. */
final class MessageCredentialStore {
    private final SharedPreferences prefs;
    private static final String ALIAS="torsepan-online-push-v1";
    private MessageCredentialStore(Context c){prefs=c.getSharedPreferences("message-credentials",Context.MODE_PRIVATE);}
    static MessageCredentialStore get(Context c){return new MessageCredentialStore(c.getApplicationContext());}
    private SecretKey key() throws Exception {
        KeyStore store=KeyStore.getInstance("AndroidKeyStore");store.load(null);
        if(!store.containsAlias(ALIAS)){
            KeyGenerator generator=KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES,"AndroidKeyStore");
            generator.init(new KeyGenParameterSpec.Builder(ALIAS,KeyProperties.PURPOSE_ENCRYPT|KeyProperties.PURPOSE_DECRYPT).setBlockModes(KeyProperties.BLOCK_MODE_GCM).setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE).build());generator.generateKey();
        }
        return (SecretKey)store.getKey(ALIAS,null);
    }
    void put(String name,String value) throws Exception {
        Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");cipher.init(Cipher.ENCRYPT_MODE,key());
        String encrypted=Base64.encodeToString(cipher.getIV(),Base64.NO_WRAP)+"."+Base64.encodeToString(cipher.doFinal(value.getBytes(java.nio.charset.StandardCharsets.UTF_8)),Base64.NO_WRAP);
        if(!prefs.edit().putString(name,encrypted).commit())throw new java.io.IOException("Could not save notification credential");
    }
    String get(String name) throws Exception {
        String value=prefs.getString(name,"");if(value.isEmpty())return "";
        String[] parts=value.split("\\.",2);Cipher cipher=Cipher.getInstance("AES/GCM/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE,key(),new GCMParameterSpec(128,Base64.decode(parts[0],Base64.NO_WRAP)));
        return new String(cipher.doFinal(Base64.decode(parts[1],Base64.NO_WRAP)),java.nio.charset.StandardCharsets.UTF_8);
    }
    synchronized String device() throws Exception {
        String value=prefs.getString("device","");if(!value.isEmpty())return value;
        value=UUID.randomUUID().toString();if(!prefs.edit().putString("device",value).commit())throw new java.io.IOException("Could not save notification device");return value;
    }
}
