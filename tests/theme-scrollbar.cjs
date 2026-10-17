// Reproduces the classic RTL scrollbar gutter used by Android WebView.
const assert=require('node:assert/strict');
const {chromium}=require(process.env.THEME_PLAYWRIGHT||'playwright');
(async()=>{
 const b=await chromium.launch({headless:true,executablePath:process.env.THEME_CHROMIUM,args:JSON.parse(process.env.THEME_CHROMIUM_ARGS||'[]')});
 try{
  const p=await b.newPage();const {token}=await(await fetch('http://127.0.0.1:5169/api/auth/login',{method:'POST'})).json();
  await p.addInitScript(t=>localStorage.setItem('access_token',t),token);
  for(const width of [360,390,768]){
   await p.setViewportSize({width,height:670});await p.goto('http://127.0.0.1:5170');await p.locator('.dashboard-tabs').waitFor();
   const measure=()=>p.evaluate(()=>{
    const c=document.querySelector('.content'),m=document.querySelector('.main').getBoundingClientRect(),photo=document.querySelector('.workshop-panorama').getBoundingClientRect();
    return{gutter:c.offsetWidth-c.clientWidth,left:photo.left-m.left,right:m.right-photo.right};
   });
   const legacy=await p.addStyleTag({content:'.theme-shell>.main>.content{scrollbar-width:auto!important;scrollbar-gutter:stable!important}.theme-shell>.main>.content::-webkit-scrollbar{display:block!important;width:10px!important}'});
   const before=await measure();assert(before.gutter>0,'classic scrollbar reproduction');assert.equal(before.left,before.gutter,'gutter displaces RTL photograph');
   await legacy.evaluate(n=>n.remove());
   assert.deepEqual(await measure(),{gutter:0,left:0,right:0},'photo must cover the full canvas after fix');
   const rail=await p.locator('.mobile-nav-rail').boundingBox();
   await p.locator('.content').evaluate(n=>{n.style.scrollBehavior='auto';n.scrollTop=100});
   assert(await p.locator('.content').evaluate(n=>n.scrollTop)>0,'touch content must remain scrollable');
   assert.deepEqual(await p.locator('.mobile-nav-rail').boundingBox(),rail,'menu must stay fixed');
   console.log('PASS '+width+'px: reproduced '+before.gutter+'px legacy gutter, removed gap, scrolling and fixed rail preserved');
  }
 }finally{await b.close()}
})().catch(e=>{console.error(e);process.exit(1)});
