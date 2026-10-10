// Usage: PHOTO_BROWSER=/path/to/chrome node tests/PhotoUploadSmoke/photo-upload.cjs /path/to/rendered-passport.html
const fs=require('fs'),path=require('path'),assert=require('node:assert/strict'),{chromium}=require('playwright');
(async()=>{
 const root=path.resolve(__dirname,'../..'),fragment=fs.readFileSync(process.argv[2],'utf8');
 const browser=await chromium.launch({headless:true,executablePath:process.env.PHOTO_BROWSER,args:['--no-sandbox']});
 try {
 const page=await browser.newPage({viewport:{width:390,height:900},isMobile:true,hasTouch:true});
 await page.route('https://fixture.invalid/**',r=>r.fulfill({body:'<html></html>',contentType:'text/html'}));await page.goto('https://fixture.invalid/');
 const css=fs.readFileSync(path.join(root,'TORSEPAN.Panel/obj/Debug/net10.0/scopedcss/projectbundle/TORSEPAN.Panel.bundle.scp.css'),'utf8');
 await page.setContent('<html><style>'+css+'</style>'+fragment+'</html>');
 await page.addScriptTag({content:fs.readFileSync(path.join(root,'TORSEPAN.Panel/wwwroot/handpan-photos.js'),'utf8')});
 const gallery=page.locator('input[id^="handpan-photo-gallery"]'),camera=page.locator('input[id^="handpan-photo-camera"]');
 assert.equal(await gallery.count(),1);assert.equal(await gallery.getAttribute('multiple'),'');assert.equal(await gallery.getAttribute('accept'),'image/*');
 const fileData=Buffer.from(await page.evaluate(async()=>{const c=document.createElement('canvas');c.width=1600;c.height=1200;const ctx=c.getContext('2d');ctx.fillStyle='#285f89';ctx.fillRect(0,0,c.width,c.height);const b=await new Promise(r=>c.toBlob(r,'image/jpeg'));return Array.from(new Uint8Array(await b.arrayBuffer()));}));
 await page.evaluate(()=>{
   window.uploads=[];window.notices=[];window.statusCode=200;
   const callback={invokeMethodAsync:async(method,id)=>window.notices.push({method,id})};
   for(const input of document.querySelectorAll('input[type=file]'))window.handpanPhotos.register(input.id,callback);
   window.fetch=async(url,options)=>{
     const file=options.body.get('file'),thumb=options.body.get('thumbnail');
     window.uploads.push({url,type:file.type,thumbType:thumb.type,size:file.size,thumbSize:thumb.size});
     return new Response(window.statusCode===200?JSON.stringify({id:'photo-'+window.uploads.length}):JSON.stringify('آپلود عکس پس از خروج ساز از اتاق چسب فعال می‌شود.'),{status:window.statusCode,headers:{'Content-Type':'application/json'}});
   };
 });
 const chooser=page.waitForEvent('filechooser');await gallery.locator('..').click();const picked=await chooser;
 assert.equal(picked.isMultiple(),true);console.log('PASS native gallery tap opens multi-file picker');
 await picked.setFiles([{name:'first.jpg',mimeType:'image/jpeg',buffer:fileData},{name:'second.jpg',mimeType:'image/jpeg',buffer:fileData}]);
 await page.waitForFunction(()=>window.uploads.length===2&&!window.handpanPhotos.hasPending);
 assert.equal(await page.evaluate(()=>window.notices.length),2);
 let uploads=await page.evaluate(()=>window.uploads);assert(uploads.every(x=>x.type==='image/webp'&&x.thumbType===x.type&&x.size<5000000&&x.thumbSize<500000));
 console.log('PASS multiple gallery images upload sequentially over HTTP and notify refresh');
 await page.evaluate(()=>{window.createImageBitmap=async()=>{throw Error('mobile decode unavailable')};const original=HTMLCanvasElement.prototype.toBlob;HTMLCanvasElement.prototype.toBlob=function(cb,type,q){return original.call(this,cb,type==='image/webp'?'image/png':type,q)};});
 await gallery.setInputFiles({name:'iphone.jpg',mimeType:'image/jpeg',buffer:fileData});
 await page.waitForFunction(()=>window.uploads.length===3&&!window.handpanPhotos.hasPending);
 uploads=await page.evaluate(()=>window.uploads);assert.equal(uploads[2].type,'image/jpeg');assert.equal(uploads[2].thumbType,'image/jpeg');
 console.log('PASS image-element decoder and JPEG encoder fallback when bitmap/WebP are unavailable');
 const cameraChooser=page.waitForEvent('filechooser');await camera.locator('..').click();const cameraPicked=await cameraChooser;assert.equal(cameraPicked.isMultiple(),false);
 await cameraPicked.setFiles({name:'camera.jpg',mimeType:'image/jpeg',buffer:fileData});await page.waitForFunction(()=>window.uploads.length===4&&!window.handpanPhotos.hasPending);
 console.log('PASS camera retains direct HTTP upload and refresh');
 await page.evaluate(()=>window.statusCode=409);await gallery.setInputFiles({name:'error.jpg',mimeType:'image/jpeg',buffer:fileData});
 await page.waitForFunction(()=>window.uploads.length===5&&!window.handpanPhotos.hasPending);
 assert((await page.locator('div[id^="handpan-photo-gallery"][role=status]').textContent()).includes('پس از خروج ساز'));
 assert.equal(await gallery.isDisabled(),false);console.log('PASS API error is shown and picker remains available for retry');
 await page.evaluate(()=>{window.statusCode=200;window.torsepanConnection={connected:false}});
 await gallery.setInputFiles({name:'resume.jpg',mimeType:'image/jpeg',buffer:fileData});await page.waitForFunction(()=>window.uploads.length===6);
 assert.equal(await page.evaluate(()=>window.handpanPhotos.hasPending),true);
 await page.evaluate(()=>{window.torsepanConnection.connected=true;window.dispatchEvent(new Event('torsepan-connected'))});await page.waitForFunction(()=>!window.handpanPhotos.hasPending);
 assert.equal(await page.evaluate(()=>window.notices.length),5);console.log('PASS successful upload refreshes after circuit reconnect without re-uploading');
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exit(1)});
