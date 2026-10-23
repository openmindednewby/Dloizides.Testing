(function(){
var q=document.getElementById('q'),out=document.getElementById('qn'),zero=document.getElementById('qz');
var fo=document.getElementById('fo'),ff=document.getElementById('ff'),fk=document.getElementById('fk'),gb=document.getElementById('gb');
var shell=document.querySelector('.shell'),bytask=document.getElementById('bytask'),filters=[fo,ff,fk].filter(Boolean);
var tests=[].slice.call(document.querySelectorAll('#tree tbody.t'));
var groups=[].slice.call(document.querySelectorAll('#tree details.grp'));
var every=[].slice.call(document.querySelectorAll('#tree details, #bytask details'));
var tasks=[].slice.call(document.querySelectorAll('#bytask details.task'));
function total(){return tests.length+' tests';}
function val(s){return s?s.value:'';}
function openUp(el){while(el){if(el.tagName==='DETAILS')el.open=true;el=el.parentElement;}}
function hasTask(t,k){var ks=(t.getAttribute('data-k')||'').split(' ').filter(Boolean);return k==='-'?ks.length===0:ks.indexOf(k)>=0;}
function apply(){
var v=q.value.trim().toLowerCase(),words=v.split(/\s+/).filter(Boolean),o=val(fo),f=val(ff),k=val(fk),n=0,on=!!(v||o||f||k);
tests.forEach(function(t){var d=t.getAttribute('data-s'),hit=words.every(function(w){return d.indexOf(w)>=0;})&&(!o||t.getAttribute('data-o')===o)&&(!f||t.getAttribute('data-f')===f)&&(!k||hasTask(t,k));t.hidden=!hit;if(hit)n++;});
groups.forEach(function(g){var any=!!g.querySelector('tbody.t:not([hidden])');g.hidden=!any;g.open=on&&any;});
tasks.forEach(function(d){d.hidden=!!k&&k!=='-'&&d.getAttribute('data-k')!==k;});
out.textContent=on?(n+' of '+tests.length+' tests match'):total();
zero.hidden=!(on&&n===0);zero.textContent=zero.hidden?'':'No test matches. Clear a filter, or try a class, method, reason or task word.';
}
function group(mode){if(!gb||!bytask||!shell)return;gb.value=mode;bytask.hidden=mode!=='task';shell.hidden=mode==='task';}
out.textContent=total();
document.getElementById('openAll').addEventListener('click',function(){every.forEach(function(d){if(!d.closest('[hidden]'))d.open=true;});});
document.getElementById('closeAll').addEventListener('click',function(){every.forEach(function(d){d.open=false;});});
q.addEventListener('input',apply);
filters.forEach(function(s){s.addEventListener('change',apply);});
if(gb)gb.addEventListener('change',function(){group(gb.value);});
function reveal(){var id=decodeURIComponent(location.hash.slice(1));if(!id)return;var el=document.getElementById(id);if(!el)return;
if(shell&&shell.contains(el)){group('area');if(el.closest('[hidden]')){q.value='';filters.forEach(function(s){s.value='';});apply();}}
openUp(el);el.scrollIntoView();}
window.addEventListener('hashchange',reveal);reveal();
})();
