// Local-only isolated API for the interactive theme preview. No real accounts/data.
const http = require('node:http');
const id = n => `11111111-1111-4111-8111-${String(n).padStart(12,'0')}`;
const token = [ {alg:'none',typ:'JWT'}, {sub:id(1),unique_name:'theme-preview',FullName:'کاربر آزمایشی','http://schemas.microsoft.com/ws/2008/06/identity/claims/role':'Administrator','http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier':id(1),exp:4102444800}, 'local-fixture' ].map((v,i)=>i===2?v:Buffer.from(JSON.stringify(v)).toString('base64url')).join('.');
const orders = ['مشتری آزمایشی اول','مشتری آزمایشی دوم','مشتری آزمایشی سوم'].map((name,i)=>({id:id(10+i),customerName:name,scaleId:id(2),scaleName:'D Kurd',durationDays:30,createdAtUtc:'2026-10-01T00:00:00Z',dueAtUtc:'2026-10-31T00:00:00Z',productionStage:'در انتظار تولید',productionStatus:'در صف',completedOperations:[],reminders:[],lines:[{id:id(20+i),position:1,scaleId:id(2),scaleName:'D Kurd',designName:'ساده',quantity:[23,50,10][i],instruments:[]}]}));
const requests=[];
http.createServer(async(req,res)=>{
 const url=new URL(req.url,'http://localhost');const path=url.pathname;let input='';for await(const c of req)input+=c;
 requests.push({method:req.method,path,body:input});let data=[];
 if(path==='/__requests')data=requests;
 else if(path.endsWith('/auth/login')||path.endsWith('/auth/refresh'))data={success:true,token,accessToken:token,refreshToken:'local-fixture',userName:'theme-preview',fullName:'کاربر آزمایشی',roles:['Administrator']};
 else if(path.endsWith('/notifications/unread'))data={count:2,userId:id(1),totalIncoming:2};
 else if(path.endsWith('/notifications/push/key'))data={enabled:false};
 else if(path.endsWith('/production/dashboard'))data={queues:Array.from({length:12},(_,i)=>({stage:'مرحله آزمایشی '+(i+1),codes:['TEST-'+i],items:[{code:'TEST-'+i,daysInStage:0}]})),currentPersianMonthTitle:'مهر',persianYear:1405,persianMonth:7,selectedPersianDay:25,daysInMonth:30,firstDayOffset:2,monthlyUserOperations:[],dailyUserOperations:[],hallStock:{topBowls:12,bottomBowls:12,handpans:3}};
 else if(path.endsWith('/orders'))data=orders;
 else if(path.endsWith('/scales'))data=[{id:id(2),name:'D Kurd',isActive:true,usage:63}];
 else if(path.endsWith('/designs/types'))data=[{id:id(3),name:'ساده'}];
 else if(path.endsWith('/export-warehouse'))data=url.searchParams.has('empty')?[]:[{id:id(30),productionCode:'TEST-EXPORT-01',itemType:'bowl',itemKind:'کاسه رو',detail:'داده آزمایشی',location:4}];
 else if(path.endsWith('/me/profile'))data={id:id(1),userName:'theme-preview',fullName:'کاربر آزمایشی',roles:['Administrator']};
 else if(path.endsWith('/notifications'))data={total:0,page:1,pageSize:20,items:[]};
 res.writeHead(200,{'Content-Type':'application/json'});res.end(JSON.stringify(data));
}).listen(5169,'127.0.0.1',()=>console.log('Isolated theme fixture listening on 5169'));
