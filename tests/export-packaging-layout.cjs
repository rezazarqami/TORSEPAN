const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const {chromium}=require(process.env.TORSEPAN_PLAYWRIGHT||'playwright');
const root=path.resolve(__dirname,'..'),preview=process.env.WAREHOUSE_PREVIEW_DIR;
if(!preview)throw Error('WAREHOUSE_PREVIEW_DIR is required');
(async()=>{
 const browser=await chromium.launch({executablePath:process.env.WAREHOUSE_CHROMIUM,args:['--no-sandbox'],headless:true});
 try{
 const page=await browser.newPage();
 await page.route('http://fixture/**',route=>{const p=new URL(route.request().url()).pathname;if(p.startsWith('/images/flags/'))return route.fulfill({contentType:'image/svg+xml',body:fs.readFileSync(path.join(root,'TORSEPAN.Panel/wwwroot',p))});return route.fulfill({body:''});});
 const styles=['vendor/bootstrap/bootstrap.rtl.min.css','app.css','theme.css','theme-pages.css','theme-palette.css','navigation-shell.css'].map(p=>fs.readFileSync(path.join(root,'TORSEPAN.Panel/wwwroot',p),'utf8')).join('\n')+'\n'+fs.readFileSync(path.join(root,'TORSEPAN.Panel/obj/Debug/net10.0/scopedcss/bundle/TORSEPAN.Panel.styles.css'),'utf8');
 for(const width of [320,360,412,768,1280])for(const state of ['export-packaging','export-packaging-selected']){
  await page.setViewportSize({width,height:1000});
  await page.setContent(`<html lang="fa" dir="rtl"><head><base href="http://fixture/"><meta name="viewport" content="width=device-width,initial-scale=1"><style>${styles}</style></head><body><div class="layout theme-shell"><div class="main"><main class="content">${fs.readFileSync(path.join(preview,state+'.html'),'utf8')}</main></div></div></body></html>`);
  const picker=page.locator('.export-packaging-destinations'),cards=picker.locator('button');
  assert.equal(await cards.count(),6);
  await page.waitForFunction(()=>[...document.querySelectorAll('.export-packaging-destination img')].every(i=>i.complete&&i.naturalWidth>0));
  const grid=await page.locator('.export-packaging-grid').evaluate(e=>getComputedStyle(e).gridTemplateColumns.split(' ').length);assert.equal(grid,3,'three columns at '+width);
  const boxes=await cards.evaluateAll(es=>es.map(e=>{const r=e.getBoundingClientRect();return {x:r.x,y:r.y,right:r.right};}));assert(boxes.every(b=>b.x>=0&&b.right<=width),'cards fit viewport');assert.equal(new Set(boxes.map(b=>Math.round(b.y))).size,2,'two destination rows');
  assert.equal(await picker.locator('[aria-pressed="true"]').count(),state.endsWith('selected')?1:0);
  const ratio=await picker.locator('h3').evaluate(e=>{function lum(s){const rgb=s.match(/[\d.]+/g).slice(0,3).map(Number).map(v=>{v/=255;return v<=.04045?v/12.92:((v+.055)/1.055)**2.4});return rgb.reduce((a,v,i)=>a+v*[.2126,.7152,.0722][i],0)}let p=e.parentElement;while(p&&getComputedStyle(p).backgroundColor==='rgba(0, 0, 0, 0)')p=p.parentElement;const f=lum(getComputedStyle(e).color),b=lum(getComputedStyle(p).backgroundColor);return (Math.max(f,b)+.05)/(Math.min(f,b)+.05);});assert(ratio>=4.5,'destination title is readable');
  if(width===360&&state.endsWith('selected'))await picker.screenshot({path:path.join(preview,'export-packaging-mobile.png')});
  console.log('PASS packaging flags, three columns, two rows, selection and readable title',width,state);
 }
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
