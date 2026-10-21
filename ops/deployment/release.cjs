const fs=require('node:fs');
const {execFileSync}=require('node:child_process');
const marker='torsepan-selective-release-v1';
const bootstrap=()=>JSON.parse(fs.readFileSync('ops/deployment/baselines.json','utf8'));
const sha=()=>execFileSync('git',['rev-parse','HEAD'],{encoding:'utf8'}).trim();
const delay=ms=>new Promise(resolve=>setTimeout(resolve,ms));

function latestRuns(runs){
 const selected=new Map();
 for(const run of runs){
  const current=selected.get(run.path);
  if(!current || run.run_number>current.run_number ||
     (run.run_number===current.run_number && run.run_attempt>current.run_attempt))selected.set(run.path,run);
 }
 return [...selected.values()];
}
function checkRuns(runs){
 const ci=latestRuns(runs.filter(r=>r.event==='push'&&r.path!=='.github/workflows/selective-liara.yml'));
 if(!ci.some(r=>r.path==='.github/workflows/build.yml'))return {ready:false};
 const failed=ci.filter(r=>r.status==='completed'&&r.conclusion!=='success');
 if(failed.length)throw new Error('CI blocked release: '+failed.map(r=>`${r.name}: ${r.conclusion}`).join(', '));
 return {ready:ci.every(r=>r.status==='completed'),ci};
}
async function baseline(github,owner,repo,product){
 const entries=await github.paginate(github.rest.repos.listDeployments,{owner,repo,environment:`liara-${product}`,per_page:100});
 for(const entry of entries){
  let payload=entry.payload;if(typeof payload==='string'){try{payload=JSON.parse(payload)}catch{continue}}
  if(payload?.managed_by!==marker)continue;
  const statuses=await github.rest.repos.listDeploymentStatuses({owner,repo,deployment_id:entry.id,per_page:1});
  if(statuses.data[0]?.state==='success')return entry.sha;
 }
 const settings=bootstrap()[product];
 if(settings?.verified!==true)throw new Error(`The actual deployed ${product} bootstrap source is not verified yet`);
 const initial=settings.sha;
 if(!/^[a-f0-9]{40}$/.test(initial||''))throw new Error(`Missing verified ${product} bootstrap SHA`);
 return initial;
}
async function prepare({github,context,core}){
 const {owner,repo}=context.repo,head=sha();
 const branch=await github.rest.repos.getBranch({owner,repo,branch:'master'});
 if(branch.data.commit.sha!==head){core.setOutput('panel','false');core.setOutput('api','false');core.notice('Superseded source; latest master will release through its own successful CI.');return;}
 let ci;
 for(let attempt=0;attempt<60;attempt++){
  const runs=await github.paginate(github.rest.actions.listWorkflowRunsForRepo,{owner,repo,head_sha:head,event:'push',per_page:100});
  ci=checkRuns(runs);if(ci.ready)break;await delay(10000);
 }
 if(!ci?.ready)throw new Error('Timed out waiting for all push CI workflows for the exact master SHA');
 const panelBase=await baseline(github,owner,repo,'panel'),apiBase=await baseline(github,owner,repo,'api');
 const plan=JSON.parse(execFileSync('python3',['ops/deployment/impact.py','--panel-base',panelBase,'--api-base',apiBase,'--head',head],{encoding:'utf8'}));
 fs.writeFileSync('.liara-release-plan.json',JSON.stringify(plan,null,2));
 core.setOutput('panel',String(plan.products.panel.deploy));core.setOutput('api',String(plan.products.api.deploy));
 await core.summary.addHeading('Selective Liara release').addTable([
  [{data:'Product',header:true},{data:'Baseline',header:true},{data:'Deploy',header:true}],
  ['Panel',panelBase,String(plan.products.panel.deploy)],['API',apiBase,String(plan.products.api.deploy)]
 ]).addRaw(`\nSource: ${head}\n`).write();
}
async function record({github,context,core},product){
 if(!['panel','api'].includes(product))throw new Error('Unknown product');
 const receipt=JSON.parse(fs.readFileSync(`.liara-${product}-receipt.json`,'utf8'));
 const head=sha();if(receipt.sha!==head||receipt.product!==product||!receipt.healthy)throw new Error('Missing successful CLI/health verification');
 const {owner,repo}=context.repo;
 const dep=await github.rest.repos.createDeployment({owner,repo,ref:head,environment:`liara-${product}`,auto_merge:false,required_contexts:[],production_environment:true,description:`Liara ${product} verified release`,payload:{managed_by:marker,product,app:receipt.app}});
 if(!dep.data.id)throw new Error('Could not create baseline deployment record');
 await github.rest.repos.createDeploymentStatus({owner,repo,deployment_id:dep.data.id,state:'success',auto_inactive:false,environment_url:receipt.health_url.replace(/\/health$/,''),log_url:`https://github.com/${owner}/${repo}/actions/runs/${context.runId}`});
 core.notice(`Recorded ${product} baseline ${head}`);
}
module.exports={latestRuns,checkRuns,baseline,prepare,record};
