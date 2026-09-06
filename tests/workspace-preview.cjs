// Local-only UI fixture. Never deployed; no production database or credentials.
const http=require('node:http');
const user='11111111-1111-1111-1111-111111111111';
const message='22222222-2222-2222-2222-222222222222';
let read=false;
const token=[{alg:'none',typ:'JWT'},{sub:user,FullName:'کاربر آزمایشی',role:'Administrator',exp:4102444800},'fixture'].map((v,i)=>i===2?v:Buffer.from(JSON.stringify(v)).toString('base64url')).join('.');
http.createServer(async(req,res)=>{
 let body='';for await(const chunk of req)body+=chunk;
 const url=new URL(req.url,'http://localhost'),path=url.pathname;
 let data={};
 if(path.endsWith('/auth/login'))data={success:true,token,refreshToken:'local-fixture',userName:'preview',fullName:'کاربر آزمایشی',roles:['Administrator']};
 else if(path.endsWith('/notifications/unread'))data={count:read?0:1};
 else if(path.endsWith('/notifications/recipients'))data=[{id:user,name:'کاربر آزمایشی'}];
 else if(path.endsWith('/read')){read=true;data={};}
 else if(path.endsWith('/notifications'))data={total:1,page:1,pageSize:20,items:[{id:message,title:'برنامه تولید این هفته',body:'همکاران عزیز، لطفاً وضعیت کارهای تکمیل‌شده را تا پایان امروز ثبت کنید.\nبا سپاس از همراهی شما.',senderName:'مدیر کارگاه',createdAt:'2026-09-02T08:00:00Z',readAt:read?'2026-09-02T09:00:00Z':null}]};
 else if(path.endsWith('/me/activity'))data={from:url.searchParams.get('from')||'2026-08-23',to:url.searchParams.get('to')||'2026-09-02',total:3,completed:3,page:1,pageSize:50,items:[['تیون کاسه رو','SS-00024','تیون کاسه تکمیل شد.'],['فاین تیون','7114','فاین تیون ساز تکمیل شد.'],['کنترل کیفیت','7142','آزمون نهایی با موفقیت انجام شد.']].map((x,i)=>({id:`33333333-3333-3333-3333-33333333333${i}`,eventDate:`2026-09-02T0${8-i}:00:00Z`,operation:x[0],code:x[1],details:x[2],duration:'۳۰ دقیقه',result:'تکمیل‌شده'}))};
 res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify(data));
}).listen(5169,'127.0.0.1',()=>console.log('Local UI fixture: 5169'));
