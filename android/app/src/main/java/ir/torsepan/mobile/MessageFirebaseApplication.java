package ir.torsepan.mobile;
import android.app.Application;
public final class MessageFirebaseApplication extends Application {
    @Override public void onCreate(){super.onCreate();NativeMessagePush.channel(this);NativeMessagePush.initialize(this);}
}
