package ir.torsepan.mobile;
import android.app.job.JobService;
import android.app.job.JobParameters;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

public final class OfflineSyncJob extends JobService {
    private final ExecutorService worker = Executors.newSingleThreadExecutor();
    @Override public boolean onStartJob(JobParameters params) {
        worker.execute(() -> { boolean retry;
            retry = new OfflineSync(this,OfflineStore.get(this)).sync();
            jobFinished(params,retry);
        }); return true;
    }
    @Override public boolean onStopJob(JobParameters params) { return true; }
    @Override public void onDestroy() { worker.shutdownNow(); super.onDestroy(); }
}
