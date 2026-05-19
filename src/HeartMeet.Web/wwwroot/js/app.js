(function(){ var t=localStorage.getItem('hm-theme'); if(t) document.documentElement.setAttribute('data-theme',t); })();

function scrollToBottom(id){ var el=document.getElementById(id); if(el) el.scrollTop=el.scrollHeight; }

function triggerFileInput(id){
    var el = document.getElementById(id);
    if(!el) return;
    // Remove and re-add to reset, then click
    el.value = '';
    el.dispatchEvent(new MouseEvent('click', {bubbles:true, cancelable:true}));
}

function showToast(msg){
    var c=document.getElementById('_t');
    if(!c){ c=document.createElement('div'); c.id='_t'; c.style.cssText='position:fixed;bottom:1.5rem;right:1.5rem;z-index:99999;display:flex;flex-direction:column;gap:.5rem'; document.body.appendChild(c); }
    var d=document.createElement('div');
    d.style.cssText='background:#1a1420;border:1px solid rgba(220,100,160,.4);border-radius:14px;padding:.75rem 1.1rem;color:#f0e8f5;font-size:.9rem;box-shadow:0 8px 32px rgba(0,0,0,.6)';
    d.textContent=msg; c.appendChild(d);
    setTimeout(function(){ d.style.transition='opacity .3s'; d.style.opacity='0'; setTimeout(function(){ d.remove(); },320); },2800);
}

// Called from Blazor to fly card away
function hmFly(dir){
    var card=document.getElementById('hmCard');
    if(!card) return;
    var sl=document.getElementById('stampLike'), sn=document.getElementById('stampNope');
    if(dir==='right' && sl) sl.style.opacity='1';
    if(dir==='left'  && sn) sn.style.opacity='1';
    card.style.transition='transform .38s ease, opacity .38s ease';
    card.style.transform='translateX('+(dir==='right'?'160%':'-160%')+') rotate('+(dir==='right'?'30deg':'-30deg')+')';
    card.style.opacity='0';
    card.style.pointerEvents='none';
}

// Init drag/swipe — triggers real button clicks
function hmInitSwipe(){
    var card=document.getElementById('hmCard');
    if(!card || card._i) return;
    card._i=true;
    var sx=0, dragging=false;

    function onStart(x){ sx=x; dragging=true; card.style.transition='none'; card.style.cursor='grabbing'; }
    function onMove(x){
        if(!dragging) return;
        var dx=x-sx;
        card.style.transform='translateX('+dx+'px) rotate('+(dx*.05)+'deg)';
        var sl=document.getElementById('stampLike'), sn=document.getElementById('stampNope');
        if(sl) sl.style.opacity = dx>55 ? '1':'0';
        if(sn) sn.style.opacity = dx<-55 ? '1':'0';
    }
    function onEnd(x){
        if(!dragging) return;
        dragging=false; card.style.cursor='grab';
        var dx=x-sx;
        var sl=document.getElementById('stampLike'), sn=document.getElementById('stampNope');
        if(dx>90){
            if(sl) sl.style.opacity='1';
            document.getElementById('blazorLike').click();
        } else if(dx<-90){
            if(sn) sn.style.opacity='1';
            document.getElementById('blazorNope').click();
        } else {
            card.style.transition='transform .28s ease'; card.style.transform='';
            if(sl) sl.style.opacity='0'; if(sn) sn.style.opacity='0';
        }
    }
    card.addEventListener('mousedown', function(e){ onStart(e.clientX); e.preventDefault(); });
    window.addEventListener('mousemove', function(e){ onMove(e.clientX); });
    window.addEventListener('mouseup',   function(e){ onEnd(e.clientX); });
    card.addEventListener('touchstart', function(e){ onStart(e.touches[0].clientX); }, {passive:true});
    card.addEventListener('touchmove',  function(e){ onMove(e.touches[0].clientX); },  {passive:true});
    card.addEventListener('touchend',   function(e){ onEnd(e.changedTouches[0].clientX); }, {passive:true});
}
