const vm=require('node:vm'),fs=require('node:fs'),assert=require('node:assert/strict');
const handlers={},notifications=[],messages=[],navigated=[];
let windows=[{url:'https://workshop.invalid/home',async navigate(url){navigated.push(url)},async focus(){navigated.push('focused')},postMessage(x){messages.push(x)}}];
const self={location:{origin:'https://workshop.invalid'},addEventListener(name,fn){handlers[name]=fn},registration:{async showNotification(title,options){notifications.push({title,...options})}},clients:{async matchAll(){return windows},async openWindow(url){navigated.push(url)}}};
vm.runInNewContext(fs.readFileSync('TORSEPAN.Panel/wwwroot/service-worker.js','utf8'),{self,URL,Set,Promise});
async function event(name,data){let promise;handlers[name]({...data,waitUntil(p){promise=p}});await promise;}
(async()=>{
 await event('push',{data:{json(){return {userId:'recipient',totalIncoming:12,tag:'message-1',url:'https://attacker.invalid/private',body:'private text'}}}});
 assert.equal(notifications.length,1);assert(!notifications[0].body.includes('private text'));
 assert.equal(notifications[0].data.url,'/notifications');assert.equal(messages[0].totalIncoming,12);
 console.log('PASS background push always displays a generic OS notification and signals open app windows');
 let closed=false;await event('notificationclick',{notification:{data:{url:'https://attacker.invalid'},close(){closed=true}}});
 assert(closed);assert.equal(navigated[0],'https://workshop.invalid/notifications');assert.equal(navigated[1],'focused');
 windows=[];await event('notificationclick',{notification:{close(){}}});assert.equal(navigated[2],'https://workshop.invalid/notifications');
 console.log('PASS notification tap opens or focuses only the local messages page');
 await event('push',{data:{json(){throw new Error('bad payload')}}});assert.equal(notifications.length,2);
 console.log('PASS empty or malformed push still displays a safe notification');
})().catch(e=>{console.error(e);process.exitCode=1});
