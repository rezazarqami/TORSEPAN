package ir.torsepan.mobile;

import static org.junit.Assert.*;
import android.content.Context;
import org.json.JSONObject;
import org.json.JSONArray;
import org.junit.Test;
import org.junit.runner.RunWith;
import org.robolectric.RobolectricTestRunner;
import org.robolectric.RuntimeEnvironment;
import org.robolectric.annotation.Config;
import java.util.UUID;
import java.time.Instant;

@RunWith(RobolectricTestRunner.class)
@Config(sdk=29)
public class OfflineStoreTest {
    // Host JVM exercises real Android SQLite. The production AndroidKeyStore codec is device-only.
    private final OfflineCodec codec = new OfflineCodec() {
        public String encrypt(String text) { return "fixture:"+text; }
        public String decrypt(String text) { return text.substring(8); }
    };
    private Context context() { return RuntimeEnvironment.getApplication(); }
    private OfflineStore create(String name) { return new OfflineStore(context(),codec,name); }
    private JSONObject snapshot(OfflineStore store) throws Exception {
        return new JSONObject().put("schema",1).put("deviceId",store.device()).put("ownerId","owner")
            .put("capturedUtc",Instant.now().toString()).put("bowls",new JSONArray()
                .put(new JSONObject().put("code","855").put("version","top").put("pairCode",JSONObject.NULL))
                .put(new JSONObject().put("code","196").put("version","bottom").put("pairCode",JSONObject.NULL)));
    }
    private void setup(OfflineStore store) throws Exception { store.put("token","fixture-token");store.put("enrolled","yes");store.installSnapshot(snapshot(store)); }
    private JSONObject op(String code,String action) throws Exception {
        return new JSONObject().put("code",code).put("action",action).put("performerId",UUID.randomUUID().toString()).put("expectedVersion","snapshot");
    }
    private String accessToken(String owner, Object roles) throws Exception {
        JSONObject claims = new JSONObject().put("sub",owner).put("role",roles);
        return "header." + android.util.Base64.encodeToString(claims.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8),
            android.util.Base64.URL_SAFE | android.util.Base64.NO_WRAP | android.util.Base64.NO_PADDING) + ".signature";
    }
    @Test public void offlineEntryRequiresAdminAndServerDeviceApproval() throws Exception {
        OfflineStore store=create(UUID.randomUUID()+".db");setup(store);
        store.put("token",accessToken("owner","Administrator"));
        OfflineSync approved=new OfflineSync(context(),store,(path,body)->{
            assertEquals("access/"+store.device(),path);
            return new OfflineSync.Reply(200,new JSONObject().put("allowed",true));
        });
        assertTrue(approved.canOpen());
        assertFalse(new OfflineSync(context(),store,(path,body)->new OfflineSync.Reply(200,new JSONObject().put("allowed",false))).canOpen());
        assertFalse(new OfflineSync(context(),store,(path,body)->new OfflineSync.Reply(403,new JSONObject())).canOpen());
        OfflineSync disconnected=new OfflineSync(context(),store,(path,body)->{throw new java.io.IOException("offline");});
        assertTrue(disconnected.canOpen());
        store.put("token",accessToken("other-admin",new JSONArray().put("Administrator")));
        assertFalse(disconnected.canOpen());
        store.put("token",accessToken("owner","Tuner"));assertFalse(approved.canOpen());
        store.put("token","");assertFalse(approved.canOpen());
        store.close();
    }
    @Test public void queueSurvivesCloseAndDependenciesSpanBothBowls() throws Exception {
        String name=UUID.randomUUID()+".db"; OfflineStore store=create(name);setup(store);
        String first=store.enqueue(op("855","tune").toString());
        String bottom=store.enqueue(op("196","bake").toString());
        String glue=store.enqueue(op("855","glue").put("pairCode","196").put("pairVersion","bottom").toString());
        String release=store.enqueue(op("196","release").toString());
        String finalTune=store.enqueue(op("855","finalTune").toString());
        JSONArray rows=store.operations();assertEquals(first,rows.getJSONObject(2).getJSONObject("body").getString("previousOperationId"));
        assertEquals(bottom,rows.getJSONObject(2).getJSONObject("body").getString("pairPreviousOperationId"));
        assertEquals(glue,rows.getJSONObject(3).getJSONObject("body").getString("previousOperationId"));
        assertEquals(release,rows.getJSONObject(4).getJSONObject("body").getString("previousOperationId"));
        assertTrue(Instant.parse(rows.getJSONObject(4).getJSONObject("body").getString("occurredUtc")).isBefore(Instant.now().plusSeconds(2)));
        store.close();store=create(name);assertEquals(5,store.pending());assertEquals(finalTune,store.operations().getJSONObject(4).getString("id"));
        store.close();context().deleteDatabase(name);
    }
    @Test public void responseLossKeepsSameIdAndRefreshWaitsForAcknowledgement() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        String id=store.enqueue(op("855","tune").toString());String original=store.get("snapshot");
        store.installSnapshot(snapshot(store).put("capturedUtc",Instant.now().plusSeconds(2).toString()));assertEquals(original,store.get("snapshot"));
        int[] tries={0};OfflineSync sync=new OfflineSync(context(),store,(path,body)->{
            if(path.equals("operations")){assertEquals(id,body.getString("operationId"));if(tries[0]++==0)throw new java.io.IOException("response lost");return new OfflineSync.Reply(200,new JSONObject().put("operationId",id).put("deviceId",store.device()).put("registrarId","owner"));}
            return new OfflineSync.Reply(200,snapshot(store));
        });
        assertTrue(sync.sync());assertEquals(1,store.pending());assertEquals(id,store.operations().getJSONObject(0).getString("id"));
        assertFalse(sync.sync());assertEquals(0,store.pending());assertEquals(0,store.operations().length());assertEquals(2,tries[0]);
        store.close();context().deleteDatabase(name);
    }
    @Test public void conflictAndAccountMismatchPreserveQueueAndSnapshot() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);String id=store.enqueue(op("855","tune").toString());
        OfflineSync sync=new OfflineSync(context(),store,(path,body)->new OfflineSync.Reply(409,new JSONObject().put("message","changed online")));
        assertFalse(sync.sync());assertEquals("blocked",store.operations().getJSONObject(0).getString("state"));assertEquals(id,store.operations().getJSONObject(0).getString("id"));
        String saved=store.get("snapshot");store.installSnapshot(snapshot(store));assertEquals(saved,store.get("snapshot"));
        try{store.installSnapshot(snapshot(store).put("ownerId","another-account"));fail("account mismatch accepted");}catch(IllegalStateException expected){}
        store.put("token","");store.enqueue(op("196","bake").toString());
        assertEquals(2,store.pending());assertFalse(store.view().getBoolean("locked"));store.close();context().deleteDatabase(name);
    }
    @Test public void unrelatedSuccessResponseCannotAcknowledgeOrEraseOutbox() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        String id=store.enqueue(op("855","tune").toString());
        OfflineSync sync=new OfflineSync(context(),store,(path,body)->new OfflineSync.Reply(200,new JSONObject().put("message","unexpected response")));
        assertTrue(sync.sync());assertEquals(1,store.pending());assertEquals(id,store.operations().getJSONObject(0).getString("id"));
        assertEquals("pending",store.operations().getJSONObject(0).getString("state"));
        store.close();context().deleteDatabase(name);
    }
    @Test public void operationRecordedDuringSnapshotDownloadPreventsBaseReplacement() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        String id=store.enqueue(op("855","tune").toString());String saved=store.get("snapshot");
        OfflineSync sync=new OfflineSync(context(),store,(path,body)->{
            if(path.equals("operations"))return new OfflineSync.Reply(200,new JSONObject().put("operationId",id).put("deviceId",store.device()).put("registrarId","owner"));
            JSONObject incoming=snapshot(store).put("capturedUtc",Instant.now().plusSeconds(2).toString());
            store.enqueue(op("855","shape").toString());return new OfflineSync.Reply(200,incoming);
        });
        assertTrue(sync.sync());assertEquals(1,store.pending());assertEquals(saved,store.get("snapshot"));
        assertEquals(2,store.operations().length());assertEquals(id,store.operations().getJSONObject(1).getJSONObject("body").getString("previousOperationId"));
        store.close();context().deleteDatabase(name);
    }

    @Test public void materialDependenciesSpanDifferentBowlsAndSurviveReopen() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        String material=UUID.randomUUID().toString();
        String first=store.enqueue(op("NEW-1","create").put("materialId",material).toString());
        String second=store.enqueue(op("NEW-2","create").put("materialId",material).toString());
        String stock=store.enqueue(op("@"+material,"stock").put("materialId",material).toString());
        String pack=store.enqueue(op("855","packaging").put("materialIds",new JSONArray().put(material)).toString());
        store.close();store=create(name);JSONArray rows=store.operations();
        assertEquals(first,rows.getJSONObject(1).getJSONObject("body").getJSONObject("materialPreviousOperations").getString(material));
        assertEquals(second,rows.getJSONObject(2).getJSONObject("body").getJSONObject("materialPreviousOperations").getString(material));
        assertEquals(stock,rows.getJSONObject(3).getJSONObject("body").getJSONObject("materialPreviousOperations").getString(material));
        assertEquals(pack,rows.getJSONObject(3).getString("id"));store.close();context().deleteDatabase(name);
    }
    @Test public void photoBytesStayDurableAndRetryUsesPhotoEndpointWithoutChangingIdentity() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        String id=store.enqueue(op("855","photo").put("image","persistent-binary").put("thumbnail","preview").put("imageHash","hash").toString());
        store.close();store=create(name);final OfflineStore reopened=store;
        assertFalse(store.view().getJSONArray("operations").getJSONObject(0).getJSONObject("body").has("image"));
        assertEquals("persistent-binary",store.operations().getJSONObject(0).getJSONObject("body").getString("image"));
        int[] attempts={0};OfflineSync sync=new OfflineSync(context(),store,(path,body)->{
            if(path.equals("photos")){
                assertEquals(id,body.getString("operationId"));assertEquals("persistent-binary",body.getString("image"));
                if(attempts[0]++==0)throw new java.io.IOException("lost response");
                return new OfflineSync.Reply(200,new JSONObject().put("operationId",id).put("deviceId",reopened.device()).put("registrarId","owner"));
            }
            return new OfflineSync.Reply(200,snapshot(reopened).put("schema",2));
        });
        assertTrue(sync.sync());assertEquals(1,store.pending());assertFalse(sync.sync());assertEquals(0,store.operations().length());assertEquals(2,attempts[0]);
        store.close();context().deleteDatabase(name);
    }
    @Test public void workspaceDependsOnLastProductionAndNewMaterialSurvivesReopen() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        store.installSnapshot(snapshot(store).put("workspace",new JSONObject().put("version","management-v1")));
        String production=store.enqueue(op("855","tune").toString());String material=UUID.randomUUID().toString();
        String created=store.enqueue(op("@materials","workspace").put("workspaceModule","materials").put("workspaceAction","save").put("workspaceData",new JSONObject().put("id",material)).toString());
        String bowl=store.enqueue(op("NEW","create").put("materialId",material).toString());store.close();store=create(name);
        JSONObject body=store.operations().getJSONObject(1).getJSONObject("body");assertEquals(production,body.getString("workspacePreviousOperationId"));assertEquals("management-v1",body.getString("workspaceExpectedVersion"));
        assertEquals(created,store.operations().getJSONObject(2).getJSONObject("body").getJSONObject("materialPreviousOperations").getString(material));assertEquals(bowl,store.operations().getJSONObject(2).getString("id"));
        store.close();context().deleteDatabase(name);
    }


    @Test public void largeEncryptedValuesDoNotDependOnCursorWindow() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);String text=new String(new char[3_000_000]).replace('\0','x');store.put("large",text);assertEquals(text,store.get("large"));store.close();context().deleteDatabase(name);
    }
    @Test public void failedSecondBatchItemRollsBackFirstCommit() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);
        try{store.enqueueBatch(new JSONArray().put(op("855","tune")).put(new JSONObject().put("action","tune")));fail("invalid second item must fail batch");}catch(org.json.JSONException expected){}
        assertEquals(0,store.operations().length());store.close();context().deleteDatabase(name);
    }
    @Test public void staleBrowserCaptureDoesNotOverwriteRotatedNativeSession() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);store.captureBrowserSession("browser-old","refresh-old");store.saveSession("native-new","refresh-new");store.captureBrowserSession("browser-old","refresh-old");assertEquals("native-new",store.get("token"));assertEquals("refresh-new",store.get("refresh_token"));store.close();context().deleteDatabase(name);
    }
    @Test public void photoClientIdentityDiffersFromDurableOperationIdentity() throws Exception {
        String name=UUID.randomUUID()+".db";OfflineStore store=create(name);setup(store);String photo=UUID.randomUUID().toString();String returned=store.enqueuePhoto(op("855","photo").put("photoId",photo).put("image","image").put("thumbnail","thumb").toString());assertEquals(photo,returned);assertNotEquals(photo,store.operations().getJSONObject(0).getString("id"));assertEquals("data:image/jpeg;base64,image",store.photoData(photo,false));store.close();context().deleteDatabase(name);
    }
}
