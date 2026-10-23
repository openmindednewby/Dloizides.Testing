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
var kq=document.getElementById('fkq'),kl=document.getElementById('fkl'),act=-1;
var kopts=kl?[].slice.call(kl.querySelectorAll('[role=option]')):[];
function kshown(){return kopts.filter(function(o){return !o.hidden;});}
function kopen(on){if(!kl)return;kl.hidden=!on;kq.setAttribute('aria-expanded',on?'true':'false');if(!on)kq.removeAttribute('aria-activedescendant');}
function kmark(i){var s=kshown();act=s.length?(i+s.length)%s.length:-1;kopts.forEach(function(o){o.classList.remove('act');o.setAttribute('aria-selected','false');});
if(act<0){kq.removeAttribute('aria-activedescendant');return;}s[act].classList.add('act');s[act].setAttribute('aria-selected','true');kq.setAttribute('aria-activedescendant',s[act].id);s[act].scrollIntoView({block:'nearest'});}
function knarrow(){var v=kq.value.trim().toLowerCase();kopts.forEach(function(o){o.hidden=!!v&&o.textContent.toLowerCase().indexOf(v)<0;});kopen(true);kmark(0);}
function klabel(){var cur=kopts.filter(function(o){return o.getAttribute('data-v')===fk.value;})[0];kq.value=fk.value&&cur?cur.textContent:'';}
function kpick(o){fk.value=o.getAttribute('data-v');klabel();kopen(false);apply();}
if(kq&&kl&&fk){
kq.addEventListener('input',function(){if(!kq.value.trim()&&fk.value){fk.value='';apply();}knarrow();});
kq.addEventListener('focus',function(){kq.select();knarrow();});
kq.addEventListener('keydown',function(e){var s=kshown();
if(e.key==='ArrowDown'){e.preventDefault();if(kl.hidden)knarrow();else kmark(act+1);}
else if(e.key==='ArrowUp'){e.preventDefault();if(kl.hidden)knarrow();else kmark(act-1);}
else if(e.key==='Enter'){if(!kl.hidden&&act>=0&&s[act]){e.preventDefault();kpick(s[act]);}}
else if(e.key==='Escape'){if(!kl.hidden){e.preventDefault();kopen(false);klabel();}}
else if(e.key==='Tab'){kopen(false);klabel();}});
kq.addEventListener('blur',function(){setTimeout(function(){kopen(false);klabel();},120);});
kopts.forEach(function(o){o.addEventListener('mousedown',function(e){e.preventDefault();kpick(o);});});}
function reveal(){var id=decodeURIComponent(location.hash.slice(1));if(!id)return;var el=document.getElementById(id);if(!el)return;
if(shell&&shell.contains(el)){group('area');if(el.closest('[hidden]')){q.value='';filters.forEach(function(s){s.value='';});if(kq)kq.value='';apply();}}
openUp(el);el.scrollIntoView();}
window.addEventListener('hashchange',reveal);reveal();
})();
