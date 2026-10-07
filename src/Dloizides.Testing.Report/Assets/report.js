(function(){
var q=document.getElementById('q'),out=document.getElementById('qn');
var tests=[].slice.call(document.querySelectorAll('tbody.t'));
var feats=[].slice.call(document.querySelectorAll('details.feat'));
var initial=feats.map(function(f){return f.open;});
function total(){return tests.length+' tests';}
out.textContent=total();
q.addEventListener('input',function(){
var v=q.value.trim().toLowerCase(),n=0;
tests.forEach(function(t){var hit=!v||t.getAttribute('data-s').indexOf(v)>=0;t.hidden=!hit;if(hit)n++;});
feats.forEach(function(f,i){var any=!!f.querySelector('tbody.t:not([hidden])');f.hidden=!any;f.open=v?any:initial[i];});
[].forEach.call(document.querySelectorAll('tbody.mh'),function(h){h.hidden=!h.parentNode.querySelector('tbody.t[data-g="'+h.getAttribute('data-g')+'"]:not([hidden])');});
[].forEach.call(document.querySelectorAll('section.setd'),function(s){s.hidden=!s.querySelector('details.feat:not([hidden])');});
out.textContent=v?(n+' of '+tests.length+' tests match'):total();
});
function reveal(){var id=location.hash.slice(1);if(!id)return;var el=document.getElementById(id);if(!el)return;var d=el.closest('details.feat');if(d)d.open=true;el.scrollIntoView();}
window.addEventListener('hashchange',reveal);reveal();
})();
