const assert=require("node:assert/strict"),fs=require("node:fs"),vm=require("node:vm"),path=require("node:path");
const base=path.resolve(__dirname,"../TORSEPAN.Panel/wwwroot");
function events() {
    const listeners = new Map();
    return {addEventListener(name,fn){if(!listeners.has(name))listeners.set(name,[]);listeners.get(name).push(fn);},
        dispatchEvent(e){for(const fn of listeners.get(e.type)||[])fn(e);}};
}
const tick = async()=>{for(let i=0;i<30;i++)await Promise.resolve();};
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
        DotNet:{async invokeMethodAsync(){state.probes++;if(probeResult instanceof Error)throw probeResult;if(typeof probeResult==="function")return await probeResult();return Date.now();}},
        Blazor:{start(c){config=c;return Promise.resolve();},
            async reconnect(){state.calls++;if(result instanceof Error)throw result;return typeof result==="function"?await result():result;},
            async pauseCircuit(){state.pauses++;config.circuit.reconnectionHandler.onConnectionDown(undefined,undefined,true);return true;},
            async resumeCircuit(){state.resumes++;return false;}}};
    vm.runInNewContext(fs.readFileSync(path.join(base,"connection.js"),"utf8"),context);
    async function run(ms){const entry=[...timers].find(([,v])=>v.ms===ms);if(entry){timers.delete(entry[0]);await entry[1].fn();await tick();}}
    return {win,doc,state,context,elements,timers,get config(){return config;},run,advance(ms){now+=ms;}};
}
(async()=>{
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
    h.advance(3000);h.doc.hidden=false;h.doc.dispatchEvent({type:"visibilitychange"});await h.run(200);
    assert.equal(h.state.probes,1);assert.equal(h.state.resumes,0);assert.equal(h.win.torsepanConnection.connected,true);assert.equal(h.state.reloads,0);
    console.log("PASS background return verifies the circuit without proactive pause");
    const frozen=connectionEnv(true,new Error("frozen circuit"));await tick();frozen.doc.hidden=true;frozen.doc.dispatchEvent({type:"visibilitychange"});
    frozen.advance(3000);frozen.doc.hidden=false;frozen.doc.dispatchEvent({type:"visibilitychange"});await frozen.run(200);await frozen.run(350);
    assert.equal(frozen.state.probes,2);assert.equal(frozen.state.reloads,1);
    console.log("PASS an unresponsive restored circuit reloads the same route");
    const cached=connectionEnv();await tick();cached.win.dispatchEvent({type:"pageshow",persisted:true});await cached.run(200);assert.equal(cached.state.reloads,0);assert.equal(cached.state.probes,1);
    console.log("PASS browser back-forward cache verifies the restored circuit without a forced reload");
    const short=connectionEnv();await tick();short.doc.hidden=true;short.doc.dispatchEvent({type:"visibilitychange"});
    short.advance(500);short.doc.hidden=false;short.doc.dispatchEvent({type:"visibilitychange"});
    short.win.dispatchEvent({type:"focus"});short.win.dispatchEvent({type:"online"});await short.run(200);
    assert.equal(short.state.probes,1);assert.equal(short.state.reloads,0);
    console.log("PASS even short app switches probe once across simultaneous lifecycle events");
    const hung=connectionEnv(()=>new Promise(()=>{}));await tick();hung.config.circuit.reconnectionHandler.onConnectionDown();
    const stuck=hung.run(0);await tick();await hung.run(8000);await stuck;
    assert.equal(hung.state.calls,1);assert.equal(hung.state.reloads,1);
    console.log("PASS stalled reconnect has a deadline and cannot freeze recovery indefinitely");
    const blocked=connectionEnv(false);await tick();blocked.win.handpanPhotos={hasPending:true};
    blocked.config.circuit.reconnectionHandler.onConnectionDown();await blocked.run(0);assert.equal(blocked.state.reloads,0);
    blocked.context.Blazor.reconnect=async()=>{blocked.state.calls++;return true;};
    blocked.win.dispatchEvent({type:"online"});await blocked.run(0);assert.equal(blocked.win.torsepanConnection.connected,true);
    console.log("PASS terminal recovery can retry after network restoration while keeping pending photos");
    const offlineReturn=connectionEnv(true,new Error("offline"));await tick();offlineReturn.context.navigator.onLine=false;
    offlineReturn.win.dispatchEvent({type:"focus"});await offlineReturn.run(200);assert.equal(offlineReturn.state.reloads,0);assert.equal(offlineReturn.state.probes,0);
    console.log("PASS offline foreground return never reloads or probes a dead network");
    const stalledProbe=connectionEnv(true,()=>new Promise(()=>{}));await tick();
    stalledProbe.win.dispatchEvent({type:"focus"});const firstProbe=stalledProbe.run(200);await tick();await stalledProbe.run(2500);await firstProbe;
    const nextProbe=stalledProbe.run(350);await tick();await stalledProbe.run(2500);await nextProbe;
    assert.equal(stalledProbe.state.probes,2);assert.equal(stalledProbe.state.reloads,1);
    console.log("PASS a silent half-open socket cannot leave the restored page frozen");
    const minutes=connectionEnv();await tick();minutes.doc.hidden=true;minutes.doc.dispatchEvent({type:"visibilitychange"});
    minutes.advance(180000);minutes.doc.hidden=false;minutes.doc.dispatchEvent({type:"resume"});minutes.win.dispatchEvent({type:"pageshow",persisted:true});await minutes.run(200);
    assert.equal(minutes.state.probes,1);assert.equal(minutes.state.reloads,0);
    console.log("PASS a three-minute background return recovers without discarding the retained session");
    const late=connectionEnv(()=>new Promise(()=>{}));await tick();late.config.circuit.reconnectionHandler.onConnectionDown();const waiting=late.run(0);await tick();
    late.doc.hidden=true;late.doc.dispatchEvent({type:"visibilitychange"});await late.run(8000);await waiting;assert.equal(late.state.reloads,0);
    late.context.Blazor.reconnect=async()=>true;late.doc.hidden=false;late.doc.dispatchEvent({type:"visibilitychange"});await late.run(0);
    assert.equal(late.win.torsepanConnection.connected,true);assert.equal(late.state.reloads,0);
    console.log("PASS stale reconnect completion while backgrounded does not poison the next foreground recovery");
})().catch(error=>{console.error(error);process.exitCode=1;});
