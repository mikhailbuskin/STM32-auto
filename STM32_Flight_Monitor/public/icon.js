
L.MSIcon = L.Icon.extend({
  options: {
    iconSize: new L.Point(25, 41),
    iconAnchor: new L.Point(12, 41),
    popupAnchor: new L.Point(1, -34),
    shadowSize: new L.Point(41, 41),
  },

  initialize(name = null, options = {}) {
    // colors processing
    // let ic = name == 'green' ? iconGreen : icon;
    // let ic2 = name == 'green' ? iconGreen2 : icon2;
    if (name == 'green') name = null;

    this._name = name;
    options = L.Util.extend(options || {}, {
      iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
      iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
      shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
      shadowRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
    });
    return L.Icon.prototype.initialize.call(this, options);
  },

  // we override creation to put icon into container,
  // so we can add additional items
  createIcon(oldIcon) {
    let icon = this._createIcon('icon', oldIcon);
    let pan = document.createElement('div');
    pan.appendChild(icon);
    icon.style.marginLeft = 0;
    icon.style.marginTop = 0;

    // marker SYMBOL
    if (this._name != null) {
      let label = document.createElement('div');
      label.className = 'leaflet-marker-icon--label';
      pan.appendChild(label);
      if (this._name == 'selected') {
        label.className += ' leaflet-marker-icon--label__selected';
      } else if (this._name == 'done') {
        label.className += ' leaflet-marker-icon--label__icon';
        label.innerHTML = '<span class="ms-icon material-icons ms-icon--button">done</span>';
      } else if (this._name == 'blank') {
        label.innerHTML = '';
        pan.removeChild(label);
      } else { label.innerHTML = this._name; }
      // save for later
      this._label = label;
    }

    this._setIconStyles(pan, 'icon');
    return pan;
  },

  // color
  update(name, done) {
    if (name) {
      this._label.innerHTML = name;
    }
    if (done != null) {
      // done
      if (done == "done" || done == "") {
        this._label.className = 'leaflet-marker-icon--label ' + (done == "done" ? 'leaflet-marker-icon--label__done':'');
      }
      // color
      else
      {
        this._label.className = 'leaflet-marker-icon--label';
        this._label.style.backgroundColor = done;
      }
    }
  }

});
