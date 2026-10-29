const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
test('mobile input keeps fields above the persistent dock and tracks the visual viewport',()=>{
 const events=()=>({handlers:{},addEventListener(n,fn){this.handlers[n]=fn}});
 const doc=events(),view=events(),win=events(),classes=new Set(),frames=[],styles=new Map();let scrolls=0,fieldBottom=746;
 class Element {matches(){return true}getBoundingClientRect(){return {top:fieldBottom-46,bottom:fieldBottom}}scrollIntoView(){scrolls++}}
 doc.documentElement={classList:{toggle(n,on){on?classes.add(n):classes.delete(n)}},style:{setProperty(n,v){styles.set(n,v)}}};doc.activeElement=new Element();
 doc.querySelector=()=>({getBoundingClientRect:()=>({height:82})});view.height=440;view.offsetTop=0;view.scale=1;win.visualViewport=view;
 let mobile=true;
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../TORSEPAN.Panel/wwwroot/mobile-input.js'),'utf8'),{window:win,document:doc,HTMLElement:Element,matchMedia:()=>({matches:mobile}),innerHeight:800,requestAnimationFrame:fn=>frames.push(fn),cancelAnimationFrame(){}});
 const flush=()=>{while(frames.length)frames.shift()()};
 flush();assert.equal(styles.get('--tp-viewport-bottom'),'360px');assert.equal(scrolls,1);
 doc.handlers.focusin();flush();assert(classes.has('mobile-input-active'));assert.equal(scrolls,2);
 // This input fits above the keyboard but would be covered by the dock.
 fieldBottom=400;view.handlers.resize();flush();assert.equal(scrolls,3);
 view.offsetTop=30;view.handlers.scroll();flush();assert.equal(styles.get('--tp-viewport-bottom'),'330px');
 doc.activeElement={};doc.handlers.focusout();flush();assert(!classes.has('mobile-input-active'));
 view.height=800;view.offsetTop=0;view.handlers.resize();flush();assert.equal(styles.get('--tp-viewport-bottom'),'0px');
 view.height=400;view.scale=2;view.handlers.resize();flush();assert.equal(styles.get('--tp-viewport-bottom'),'0px','pinch zoom must not be mistaken for a keyboard');
 mobile=false;doc.activeElement=new Element();doc.handlers.focusin();flush();assert(!classes.has('mobile-input-active'));assert.equal(styles.get('--tp-viewport-bottom'),'0px');
});
