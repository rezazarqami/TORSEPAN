package ir.torsepan.mobile;
import android.app.job.*;
import android.content.*;
public final class MessageRegistrationJob extends JobService {
    static void schedule(Context c){c.getSystemService(JobScheduler.class).schedule(new JobInfo.Builder(7330,new ComponentName(c,MessageRegistrationJob.class)).setRequiredNetworkType(JobInfo.NETWORK_TYPE_ANY).setPersisted(true).setBackoffCriteria(30000,JobInfo.BACKOFF_POLICY_EXPONENTIAL).build());}
    @Override public boolean onStartJob(JobParameters p){new Thread(()->jobFinished(p,NativeMessagePush.register(this)),"message-push-registration").start();return true;}
    @Override public boolean onStopJob(JobParameters p){return true;}
}
