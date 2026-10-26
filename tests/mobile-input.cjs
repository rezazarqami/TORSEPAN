const test=require('node:test'),assert=require('node:assert/strict'),vm=require('node:vm'),fs=require('node:fs'),path=require('node:path');
test('mobile input keeps the active field visible, removes dock and restores it after blur',()=>{
 const events=()=>({handlers:{},addEventListener(n,fn){this.handlers[n]=fn}});
 const doc=events(),view=events(),win=events(),classes=new Set(),frames=[];let scrolls=0;
 class Element {matches(){return true}getBoundingClientRect(){return {top:700,bottom:746}}scrollIntoView(){scrolls++}}
 doc.documentElement={classList:{toggle(n,on){on?classes.add(n):classes.delete(n)}}};doc.activeElement=new Element();view.height=440;view.offsetTop=0;win.visualViewport=view;
 let mobile=true;
 vm.runInNewContext(fs.readFileSync(path.join(__dirname,'../TORSEPAN.Panel/wwwroot/mobile-input.js'),'utf8'),{window:win,document:doc,HTMLElement:Element,matchMedia:()=>({matches:mobile}),innerHeight:800,requestAnimationFrame:fn=>frames.push(fn),cancelAnimationFrame(){}});
 const flush=()=>{while(frames.length)frames.shift()()};
 doc.handlers.focusin();flush();assert(classes.has('mobile-input-active'));assert.equal(scrolls,1);
 view.handlers.resize();flush();assert.equal(scrolls,2);
 doc.activeElement={};doc.handlers.focusout();flush();assert(!classes.has('mobile-input-active'));
 mobile=false;doc.activeElement=new Element();doc.handlers.focusin();flush();assert(!classes.has('mobile-input-active'));
});
