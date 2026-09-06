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
function connectionEnv(result=true) {
    const win=events(),doc=events(),timers=new Map(),elements=new Map();let seq=0,config;
    for(const id of ["torsepan-connection","torsepan-connection-text","torsepan-connection-retry","torsepan-connection-reload"])
        elements.set(id,{...events(),hidden:true,textContent:""});
    doc.hidden=false;doc.getElementById=id=>elements.get(id);doc.querySelectorAll=()=>[];
    const state={calls:0,reloads:0,pauses:0,resumes:0,probes:0},storage=new Map();
    const context={window:win,document:doc,navigator:{onLine:true},Event,Date,Array,Boolean,Number,String,
        sessionStorage:{getItem:k=>storage.get(k),setItem:(k,v)=>storage.set(k,v)},
        setTimeout(fn,ms){timers.set(++seq,{fn,ms});return seq;},clearTimeout:id=>timers.delete(id),
        location:{reload(){state.reloads++;}},
        DotNet:{async invokeMethodAsync(){state.probes++;return Date.now();}},
        Blazor:{start(c){config=c;return Promise.resolve();},
            async reconnect(){state.calls++;if(result instanceof Error)throw result;return typeof result==="function"?await result():result;},
            async pauseCircuit(){state.pauses++;config.circuit.reconnectionHandler.onConnectionDown(undefined,undefined,true);return true;},
            async resumeCircuit(){state.resumes++;config.circuit.reconnectionHandler.onConnectionUp();return true;}}};
    vm.runInNewContext(fs.readFileSync(path.join(base,"connection.js"),"utf8"),context);
    async function run(ms){const entry=[...timers].find(([,v])=>v.ms===ms);if(entry){timers.delete(entry[0]);await entry[1].fn();await tick();}}
    return {win,doc,state,context,elements,timers,get config(){return config;},run};
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
    e.config.circuit.reconnectionHandler.onConnectionUp();e.config.circuit.reconnectionHandler.onConnectionDown();await e.run(0);assert.equal(e.state.reloads,1);
    console.log("PASS expired circuit recovery is automatic and reload-loop protected");
    const f=connectionEnv(false);f.win.handpanPhotos={hasPending:true};f.config.circuit.reconnectionHandler.onConnectionDown();await f.run(0);assert.equal(f.state.reloads,0);
    console.log("PASS pending camera upload blocks automatic page reload");
    const g=connectionEnv(new Error("offline"));g.context.navigator.onLine=false;g.config.circuit.reconnectionHandler.onConnectionDown();await g.run(0);assert.equal(g.state.calls,0);
    g.context.navigator.onLine=true;g.win.dispatchEvent({type:"online"});await g.run(0);assert.equal(g.state.calls,1);
    console.log("PASS offline state wakes on network restoration");
    const h=connectionEnv();h.doc.hidden=true;h.doc.dispatchEvent({type:"visibilitychange"});await tick();
    assert.equal(h.state.pauses,1);assert.equal(h.win.torsepanConnection.connected,false);
    h.doc.hidden=false;h.doc.dispatchEvent({type:"visibilitychange"});await tick();
    assert.equal(h.state.resumes,1);assert.equal(h.win.torsepanConnection.connected,true);assert.equal(h.state.reloads,0);
    console.log("PASS backgrounded mobile circuit resumes with active server controls");
    const app=fs.readFileSync(path.resolve(base,"../Components/App.razor"),"utf8");
    assert.match(app,/blazor.web.js" autostart="false"/);assert.match(app,/Assets\["connection.js"\]/);
    console.log("PASS recovery script is wired into the actual application");
})().catch(error=>{console.error(error);process.exitCode=1;});
