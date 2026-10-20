(function () {
  document.documentElement.classList.add('js');
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  var st = document.querySelector('.statement p');
  var keys = ['book.', 'stopped'];
  var words = st.getAttribute('data-words').split(' ');
  st.textContent = '';
  var spans = words.map(function (w) {
    var s = document.createElement('span'); s.className = 'w' + (keys.indexOf(w) >= 0 ? ' k' : ''); s.textContent = w;
    st.appendChild(s); st.appendChild(document.createTextNode(' '));
    return s;
  });
  var ticking = false;
  function paint() {
    ticking = false;
    var vh = window.innerHeight, r = st.getBoundingClientRect();
    var p = Math.max(0, Math.min(1, (vh * 0.85 - r.top) / (r.height + vh * 0.35)));
    spans.forEach(function (s, i) { s.classList.toggle('on', i / spans.length < p); });
  }
  function onScroll() { if (!ticking) { ticking = true; requestAnimationFrame(paint); } }
  if (reduce) spans.forEach(function (s) { s.classList.add('on'); });
  else { window.addEventListener('scroll', onScroll, { passive: true }); window.addEventListener('resize', onScroll); paint(); }

  var ua = navigator.userAgent || '';
  var isWindows = (navigator.userAgentData && navigator.userAgentData.platform === 'Windows') || /Windows/.test(ua);
  if (!isWindows) document.getElementById('platform-note').hidden = false;

  fetch('https://api.github.com/repos/kiwayu/Audlet/releases/latest', { headers: { Accept: 'application/vnd.github+json' } })
    .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
    .then(function (rel) {
      var asset = (rel.assets || []).filter(function (a) { return /^Audlet-Setup.*\.exe$/i.test(a.name); })[0];
      if (!asset) return;
      var mb = Math.round(asset.size / 1048576);
      ['dl', 'dl2', 'dl-nav'].forEach(function (id) { var a = document.getElementById(id); if (a) a.href = asset.browser_download_url; });
      ['dl-ver', 'dl2-ver'].forEach(function (id) { document.getElementById(id).textContent = rel.tag_name + ' · ' + mb + ' MB'; });
      var sum = (rel.assets || []).filter(function (a) { return /\.sha256$/i.test(a.name); })[0];
      if (!sum) return;
      return fetch(sum.browser_download_url).then(function (r) { return r.ok ? r.text() : ''; }).then(function (text) {
        var hash = (text.match(/[0-9a-f]{64}/i) || [])[0];
        if (!hash) return;
        var el = document.getElementById('sha');
        el.textContent = 'SHA-256 of the installer: ' + hash.toLowerCase();
        el.hidden = false;
      });
    })
    .catch(function () {});
})();
