(function(){
var q=document.getElementById('q'),out=document.getElementById('qn'),zero=document.getElementById('qz');
var tests=[].slice.call(document.querySelectorAll('#tree tbody.t'));
var groups=[].slice.call(document.querySelectorAll('#tree details.area,#tree details.thing,#tree details.method'));
function total(){return tests.length+' tests';}
function openUp(el){while(el){if(el.tagName==='DETAILS')el.open=true;el=el.parentElement;}}
out.textContent=total();
document.getElementById('openAll').addEventListener('click',function(){groups.forEach(function(d){if(!d.hidden)d.open=true;});});
document.getElementById('closeAll').addEventListener('click',function(){groups.forEach(function(d){d.open=false;});});
q.addEventListener('input',function(){
var v=q.value.trim().toLowerCase(),words=v.split(/\s+/).filter(Boolean),n=0;
tests.forEach(function(t){var d=t.getAttribute('data-s'),hit=words.every(function(w){return d.indexOf(w)>=0;});t.hidden=!hit;if(hit)n++;});
groups.forEach(function(g){var any=!!g.querySelector('tbody.t:not([hidden])');g.hidden=!any;g.open=!!v&&any;});
out.textContent=v?(n+' of '+tests.length+' tests match'):total();
zero.hidden=!(v&&n===0);zero.textContent=zero.hidden?'':'No test matches "'+q.value.trim()+'". Try a class, method or reason word.';
});
function reveal(){var id=decodeURIComponent(location.hash.slice(1));if(!id)return;var el=document.getElementById(id);if(!el)return;openUp(el);el.scrollIntoView();}
window.addEventListener('hashchange',reveal);reveal();
})();
