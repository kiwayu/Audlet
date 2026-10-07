(function () {
  document.documentElement.classList.add('js');
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // Statement: split into words, sharpen them as the paragraph scrolls through the viewport.
  var st = document.querySelector('.statement p');
  var keys = ['book.', 'position,', 'chapters,', 'bookmarks'];
  var words = st.getAttribute('data-words').split(' ');
  st.textContent = '';
  var spans = words.map(function (w) {
    var s = document.createElement('span'); s.className = 'w' + (keys.indexOf(w) >= 0 ? ' k' : ''); s.textContent = w;
    st.appendChild(s); st.appendChild(document.createTextNode(' '));
    return s;
  });

  // Hero screenshot settles from slightly tilted into place as it scrolls up.
  var shot = document.getElementById('shot');
  var ticking = false;
  function paint() {
    ticking = false;
    var vh = window.innerHeight;
    var r = st.getBoundingClientRect();
    var p = Math.max(0, Math.min(1, (vh * 0.85 - r.top) / (r.height + vh * 0.35)));
    spans.forEach(function (s, i) { s.classList.toggle('on', i / spans.length < p); });
    if (shot) {
      var sr = shot.getBoundingClientRect();
      var q = Math.max(0, Math.min(1, (vh - sr.top) / (vh * 0.9)));
      var e = 1 - Math.pow(1 - q, 3);
      shot.style.transform = 'perspective(1400px) rotateX(' + ((1 - e) * 5) + 'deg) scale(' + (0.97 + e * 0.03) + ')';
    }
  }
  function onScroll() { if (!ticking) { ticking = true; requestAnimationFrame(paint); } }
  if (reduce) spans.forEach(function (s) { s.classList.add('on'); });
  else { window.addEventListener('scroll', onScroll, { passive: true }); window.addEventListener('resize', onScroll); paint(); }

  // Visitors on another system get a one-line note; the button still works.
  var ua = navigator.userAgent || '';
  var isWindows = (navigator.userAgentData && navigator.userAgentData.platform === 'Windows') || /Windows/.test(ua);
  if (!isWindows) document.getElementById('platform-note').hidden = false;

  // Point the buttons at the current installer. If the API cannot be reached, the "latest release" page link stays.
  fetch('https://api.github.com/repos/kiwayu/Audlet/releases/latest', { headers: { Accept: 'application/vnd.github+json' } })
    .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
    .then(function (rel) {
      var asset = (rel.assets || []).filter(function (a) { return /^Audlet-Setup.*\.exe$/i.test(a.name); })[0];
      if (!asset) return;
      var mb = Math.round(asset.size / 1048576);
      ['dl', 'dl2', 'dl-nav'].forEach(function (id) { var a = document.getElementById(id); if (a) a.href = asset.browser_download_url; });
      ['dl-ver', 'dl2-ver'].forEach(function (id) { document.getElementById(id).textContent = rel.tag_name + ' · ' + mb + ' MB'; });
    })
    .catch(function () {});
})();
