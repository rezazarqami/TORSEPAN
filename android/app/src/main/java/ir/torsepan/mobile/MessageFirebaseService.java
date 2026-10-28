package ir.torsepan.mobile;
import com.google.firebase.messaging.*;
import java.util.Map;
public final class MessageFirebaseService extends FirebaseMessagingService {
    @Override public void onNewToken(String token){NativeMessagePush.newToken(this,token);}
    @Override public void onMessageReceived(RemoteMessage message){Map<String,String> d=message.getData();NativeMessagePush.deliver(this,d.get("kind"),d.get("userId"),d.get("enrollmentId"),d.get("messageId"));}
}
