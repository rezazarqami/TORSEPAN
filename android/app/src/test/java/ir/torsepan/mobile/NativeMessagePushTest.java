package ir.torsepan.mobile;
import static org.junit.Assert.*;
import android.app.*;
import android.content.*;
import android.app.job.JobScheduler;
import org.junit.*;
import org.junit.runner.RunWith;
import org.robolectric.*;
import org.robolectric.annotation.Config;
import java.util.UUID;
@RunWith(RobolectricTestRunner.class)
@Config(sdk=29)
public class NativeMessagePushTest {
    private Context c;private final String owner=UUID.randomUUID().toString(),enrollment=UUID.randomUUID().toString();
    @Before public void setup(){c=RuntimeEnvironment.getApplication();NativeMessagePush.prefs(c).edit().clear().putBoolean("enabled",true).putString("owner",owner).putString("enrollment",enrollment).commit();NativeMessagePush.channel(c);}
    @Test public void messageIsNormalNotificationWithNoOngoingConnection(){String id=UUID.randomUUID().toString();assertTrue(NativeMessagePush.deliver(c,"workshop-message",owner,enrollment,id));Notification n=Shadows.shadowOf(c.getSystemService(NotificationManager.class)).getAllNotifications().get(0);assertEquals(NativeMessagePush.CHANNEL,n.getChannelId());assertEquals(0,n.flags&Notification.FLAG_ONGOING_EVENT);assertTrue((n.flags&Notification.FLAG_AUTO_CANCEL)!=0);assertEquals(Notification.CATEGORY_MESSAGE,n.category);assertEquals(1,Shadows.shadowOf(c.getSystemService(NotificationManager.class)).getAllNotifications().size());assertTrue(NativeMessagePush.deliver(c,"workshop-message",owner,enrollment,id));assertEquals(1,Shadows.shadowOf(c.getSystemService(NotificationManager.class)).getAllNotifications().size());}
    @Test public void staleAccountAndEnrollmentAndOtherEventsNeverDisplay(){assertFalse(NativeMessagePush.deliver(c,"workshop-message",UUID.randomUUID().toString(),enrollment,UUID.randomUUID().toString()));assertFalse(NativeMessagePush.deliver(c,"workshop-message",owner,UUID.randomUUID().toString(),UUID.randomUUID().toString()));assertFalse(NativeMessagePush.deliver(c,"warehouse",owner,enrollment,UUID.randomUUID().toString()));assertTrue(Shadows.shadowOf(c.getSystemService(NotificationManager.class)).getAllNotifications().isEmpty());}
    @Test public void disabledMessagesDoNotDisplayOrRegister(){NativeMessagePush.prefs(c).edit().putBoolean("enabled",false).commit();assertFalse(NativeMessagePush.deliver(c,"workshop-message",owner,enrollment,UUID.randomUUID().toString()));NativeMessagePush.newToken(c,"test-token");assertTrue(c.getSystemService(JobScheduler.class).getAllPendingJobs().isEmpty());}
    @Test public void tokenRefreshQueuesOnlyShortBackgroundRegistration(){NativeMessagePush.newToken(c,"test-token");assertEquals(1,c.getSystemService(JobScheduler.class).getAllPendingJobs().size());assertEquals(0,Shadows.shadowOf(c.getSystemService(NotificationManager.class)).getAllNotifications().size());}
}
