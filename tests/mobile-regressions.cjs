const assert = require("node:assert/strict");
const fs = require("node:fs");
const vm = require("node:vm");
const path = require("node:path");
const base = path.resolve(__dirname, "../TORSEPAN.Panel/wwwroot");
function events() {
    const listeners = new Map();
    return {addEventListener(name,fn){if(!listeners.has(name))listeners.set(name,[]);listeners.get(name).push(fn);},
        dispatchEvent(e){for(const fn of listeners.get(e.type)||[])fn(e);}};
}
const tick = async()=>{for(let i=0;i<10;i++)await Promise.resolve();};
function photoEnv() {
    const win=events(),doc=events(),timers=new Map(),state={reloads:0,uploads:0,callbacks:0,closed:0};let n=0;
    const elements=new Map(),caption={textContent:"دوربین"},label={classList:{add(){},remove(){}},querySelector(){return caption;}};
    const input={id:"camera",files:[new File(["image"],"capture.jpg",{type:"image/jpeg"})],value:"capture.jpg",disabled:false,closest(){return label;}};
    elements.set("camera",input);elements.set("camera-status",{textContent:""});
    doc.hidden=false;doc.getElementById=id=>elements.get(id);
    doc.createElement=()=>({getContext:()=>({drawImage(){}}),toBlob(cb,type){cb(new Blob(["webp"],{type}));}});
    const context={window:win,document:doc,Blob,File,FormData,Uint8Array,AbortController,
        createImageBitmap:async()=>({width:4000,height:3000,close(){state.closed++;}}),
        setTimeout(fn){timers.set(++n,fn);return n;},clearTimeout(id){timers.delete(id);},
        localStorage:{getItem:()=>"test-token"},location:{reload(){state.reloads++;}},
        fetch:async()=>{state.uploads++;return {ok:true,json:async()=>({id:"123"})};}};
    vm.runInNewContext(fs.readFileSync(path.join(base,"handpan-photos.js"),"utf8"),context);
    return {api:win.handpanPhotos,win,doc,state,input,elements,context,timers};
}
function connectionEnv(result=true, probeResult=true) {
    const win=events(),doc=events(),timers=new Map(),elements=new Map();let seq=0,config,now=Date.now();
    for(const id of ["torsepan-connection","torsepan-connection-text","torsepan-connection-retry","torsepan-connection-reload"])
        elements.set(id,{...events(),hidden:true,textContent:""});
    doc.hidden=false;doc.getElementById=id=>elements.get(id);doc.querySelectorAll=()=>[];
    const state={calls:0,reloads:0,pauses:0,resumes:0,probes:0},storage=new Map();
    const context={window:win,document:doc,navigator:{onLine:true},Event,Date:{now:()=>now},Array,Boolean,Number,String,
        sessionStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v),removeItem:k=>storage.delete(k)},
        setTimeout(fn,ms){timers.set(++seq,{fn,ms});return seq;},clearTimeout:id=>timers.delete(id),
        location:{href:"https://torsepan.liara.run/warehouse/list",reload(){state.reloads++;}},
        DotNet:{async invokeMethodAsync(){state.probes++;if(probeResult instanceof Error)throw probeResult;return Date.now();}},
        Blazor:{start(c){config=c;return Promise.resolve();},
            async reconnect(){state.calls++;if(result instanceof Error)throw result;return typeof result==="function"?await result():result;},
            async pauseCircuit(){state.pauses++;config.circuit.reconnectionHandler.onConnectionDown(undefined,undefined,true);return true;},
            async resumeCircuit(){state.resumes++;config.circuit.reconnectionHandler.onConnectionUp();return true;}}};
    vm.runInNewContext(fs.readFileSync(path.join(base,"connection.js"),"utf8"),context);
    async function run(ms){const entry=[...timers].find(([,v])=>v.ms===ms);if(entry){timers.delete(entry[0]);await entry[1].fn();await tick();}}
    return {win,doc,state,context,elements,timers,get config(){return config;},run,advance(ms){now+=ms;}};
}
(async()=>{
    const p=photoEnv();
    p.api.register("camera",{invokeMethodAsync:async(name,id)=>{assert.equal(name,"CameraUploaded");assert.equal(id,"123");p.state.callbacks++;}});
    await p.api.uploadCamera(p.input,"handpan");
    assert.equal(p.state.uploads,1);assert.equal(p.state.callbacks,1);assert.equal(p.state.reloads,0);assert.equal(p.input.disabled,false);assert.equal(p.state.closed,1);
    console.log("PASS camera upload refreshes gallery without page reload");
    const q=photoEnv();q.win.torsepanConnection={connected:false};
    q.api.register("camera",{invokeMethodAsync:async()=>{q.state.callbacks++;}});
    await q.api.uploadCamera(q.input,"handpan");assert.equal(q.state.callbacks,0);assert.equal(q.api.hasPending,true);
    q.win.torsepanConnection.connected=true;q.win.dispatchEvent({type:"torsepan-connected"});await tick();
    assert.equal(q.state.callbacks,1);assert.equal(q.state.uploads,1);assert.equal(q.api.hasPending,false);
    console.log("PASS disconnected upload is retained and notified once after reconnect");
    const retry=photoEnv();
    retry.api.register("camera",{invokeMethodAsync:async()=>{if(++retry.state.callbacks===1)throw new Error("temporary gallery error");}});
    await retry.api.uploadCamera(retry.input,"handpan");assert.equal(retry.api.hasPending,true);
    for(const [id,fn] of [...retry.timers]){retry.timers.delete(id);await fn();}await tick();
    assert.equal(retry.state.callbacks,2);assert.equal(retry.state.uploads,1);assert.equal(retry.api.hasPending,false);
    console.log("PASS gallery callback retries without uploading the photo twice");
    const r=photoEnv();r.input.files=[];await r.api.uploadCamera(r.input,"handpan");assert.equal(r.state.uploads,0);assert.equal(r.input.disabled,false);
    console.log("PASS camera cancellation leaves controls usable");
    const c=connectionEnv();c.config.circuit.reconnectionHandler.onConnectionDown();await c.run(0);
    assert.equal(c.state.calls,1);assert.equal(c.state.reloads,0);assert.equal(c.win.torsepanConnection.connected,true);
    assert.equal(c.elements.get("torsepan-connection").hidden,true);
    console.log("PASS transient disconnect recovers silently and clears state");
    let finish;const d=connectionEnv(()=>new Promise(resolve=>finish=resolve));
    d.config.circuit.reconnectionHandler.onConnectionDown();const first=d.run(0);await tick();
    d.win.dispatchEvent({type:"online"});await d.run(0);assert.equal(d.state.calls,1);finish(true);await first;
    console.log("PASS simultaneous wake events do not start concurrent reconnects");
    const e=connectionEnv(false);e.config.circuit.reconnectionHandler.onConnectionDown();await e.run(0);assert.equal(e.state.reloads,1);
    e.config.circuit.reconnectionHandler.onConnectionUp();e.config.circuit.reconnectionHandler.onConnectionDown();await e.run(0);assert.equal(e.state.reloads,2);
    console.log("PASS each newly expired circuit reloads automatically after a successful recovery");
    const f=connectionEnv(false);f.win.handpanPhotos={hasPending:true};f.config.circuit.reconnectionHandler.onConnectionDown();await f.run(0);assert.equal(f.state.reloads,0);
    console.log("PASS pending camera upload blocks automatic page reload");
    const g=connectionEnv(new Error("offline"));g.context.navigator.onLine=false;g.config.circuit.reconnectionHandler.onConnectionDown();await g.run(0);assert.equal(g.state.calls,0);
    g.context.navigator.onLine=true;g.win.dispatchEvent({type:"online"});await g.run(0);assert.equal(g.state.calls,1);
    console.log("PASS offline state wakes on network restoration");
    const h=connectionEnv();h.doc.hidden=true;h.doc.dispatchEvent({type:"visibilitychange"});await tick();
    assert.equal(h.state.pauses,0);assert.equal(h.win.torsepanConnection.connected,true);
    h.advance(3000);h.doc.hidden=false;h.doc.dispatchEvent({type:"visibilitychange"});await h.run(250);
    assert.equal(h.state.probes,1);assert.equal(h.state.resumes,0);assert.equal(h.win.torsepanConnection.connected,true);assert.equal(h.state.reloads,0);
    console.log("PASS background return verifies the circuit without proactive pause");
    const frozen=connectionEnv(true,new Error("frozen circuit"));frozen.doc.hidden=true;frozen.doc.dispatchEvent({type:"visibilitychange"});
    frozen.advance(3000);frozen.doc.hidden=false;frozen.doc.dispatchEvent({type:"visibilitychange"});await frozen.run(250);await frozen.run(700);
    assert.equal(frozen.state.probes,2);assert.equal(frozen.state.reloads,1);
    console.log("PASS an unresponsive restored circuit reloads the same route");
    const cached=connectionEnv();cached.win.dispatchEvent({type:"pageshow",persisted:true});await tick();assert.equal(cached.state.reloads,1);
    console.log("PASS browser back-forward cache cannot restore a frozen page");
    const app=fs.readFileSync(path.resolve(base,"../Components/App.razor"),"utf8");
    assert.match(app,/blazor.web.js" autostart="false"/);assert.match(app,/Assets\["connection.js"\]/);
    console.log("PASS recovery script is wired into the actual application");
    const layoutCss=fs.readFileSync(path.join(base,"app.css"),"utf8");
    const warehouseCss=fs.readFileSync(path.resolve(base,"../Components/Pages/WarehouseList.razor.css"),"utf8");
    assert.match(layoutCss,/width:\s*calc\(100% - 25px\)/);assert.match(layoutCss,/margin-right:\s*25px/);
    assert.doesNotMatch(warehouseCss,/warehouse-detail[^}]*100vw/);
    console.log("PASS mobile content reserves the menu rail without clipping the left edge");
    const components=path.resolve(base,"../Components");
    const salesPage=fs.readFileSync(path.join(components,"Pages/Sales.razor"),"utf8");
    const warehousePage=fs.readFileSync(path.join(components,"Pages/WarehouseList.razor"),"utf8");
    const confirmCss=fs.readFileSync(path.join(components,"Shared/AppConfirmDialog.razor.css"),"utf8");
    const salesRollback=salesPage.slice(salesPage.indexOf("private bool RollbackBusy"));
    const warehouseRollback=warehousePage.slice(warehousePage.indexOf("private bool RollbackBusy"),warehousePage.indexOf("private async Task ToggleAsync"));
    assert.match(salesPage,/<AppConfirmDialog/);assert.match(warehousePage,/<AppConfirmDialog/);
    assert.doesNotMatch(salesRollback,/JS\.InvokeAsync/);assert.doesNotMatch(warehouseRollback,/JS\.InvokeAsync/);
    assert.match(confirmCss,/@media \(max-width: 520px\)/);
    assert.match(salesPage,/class="sale-actions-cell"/);assert.match(warehousePage,/class="warehouse-admin-cell"/);
    console.log("PASS rollback actions use the responsive in-app confirmation dialog");
})().catch(error=>{console.error(error);process.exitCode=1;});
