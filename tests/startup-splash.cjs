const fs = require('node:fs');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const source = fs.readFileSync('TORSEPAN.Panel/wwwroot/startup.js','utf8');
let ready = false, removed = 0, disconnected = 0, mutation;
const frames = [];
vm.runInNewContext(source, {
  document: { body:{}, getElementById:()=>({remove(){removed++;}}), querySelector:()=>ready?{}:null },
  MutationObserver: class { constructor(cb){mutation=cb;} observe(){} disconnect(){disconnected++;} },
  requestAnimationFrame: cb=>frames.push(cb)
});
assert.equal(removed,0,'static splash stays visible before Blazor starts');
mutation(); mutation();
assert.equal(frames.length,0,'authentication and incomplete dashboard rendering never dismiss it');
ready=true; mutation();
assert.equal(removed,0,'ready markup is painted before splash disappears');
frames.shift()();
assert.equal(removed,1); assert.equal(disconnected,1);
mutation(); assert.equal(frames.length,0,'later navigation never restarts the startup screen');
const routes=fs.readFileSync('TORSEPAN.Panel/Components/Routes.razor','utf8');
const app=fs.readFileSync('TORSEPAN.Panel/Components/App.razor','utf8');
const dashboard=fs.readFileSync('TORSEPAN.Panel/Components/Pages/Dashboard.razor','utf8');
assert.ok(app.indexOf('id="torsepan-startup"') < app.indexOf('<Routes '),'splash exists in the server HTML before the interactive circuit');
assert.ok(app.includes('<p>Loading</p>') && !routes.includes('در حال بارگذاری'));
assert.ok(routes.includes('routeData.PageType != typeof(TORSEPAN.Panel.Components.Pages.Dashboard)'));
assert.ok(dashboard.includes('else\n{\n    <span data-torsepan-startup-ready hidden>'),'dashboard marker waits for loaded data');
console.log('PASS startup splash: static boot, slow authentication/data, paint handoff, English label and one-time dismissal');
