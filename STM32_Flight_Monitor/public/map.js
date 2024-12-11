const map = L.map('map').setView([33.674394, -117.800962], 18);

// layers
const satellite = L.tileLayer('https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}', {maxZoom: 19});
const street = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {maxZoom: 19}).addTo(map);
const baseMaps = {
    'Satellite': satellite,
    'Street': street,
};
var layerControl = L.control.layers(baseMaps).addTo(map);

// click on map, to add marker
var waypoints = [];
map.on('click', function(e) {
    let wpNumber = waypoints.length + 1;
    let icon = new L.MSIcon(wpNumber);
    let marker = L.marker([e.latlng.lat, e.latlng.lng], {
        icon: icon
    }).addTo(map);
    marker.on('click', function() {
        removeWaypoint(marker);
    });
    marker._iconObject = icon;
    marker._done = false;
    waypoints.push(marker);
    // change icon
    //icon.update(null,'lightgreen');
})

// watch for drone coordinates updates
let homeMarker = null;
let droneMarker = null;
let telemetryData = null;
document.addEventListener('telemetry', function(event) {
    let tel = telemetryData = event.detail;
    if (droneMarker) {
        droneMarker.setLatLng([tel.lat, tel.lon]);
    } else {
        // init map position
        map.setView([tel.lat, tel.lon], 18);
        let icon = new L.MSIcon('✈️');//🚁
        droneMarker = L.marker([tel.lat, tel.lon], {
            icon: icon,
        }).addTo(map);
        icon.update(null, 'maroon');
    }

    // home position
    if (tel.start == 2) {   
        if (homeMarker == null) {
            let icon = new L.MSIcon('🏠');
            homeMarker = L.marker([tel.lat, tel.lon], {
                icon: icon,
            }).addTo(map);
            icon.update(null, 'lightblue');
        }
    }
    if (tel.start == 0) {
        if (homeMarker) {
            map.removeLayer(homeMarker);
            homeMarker = null;
        }
    }
});

function removeWaypoint(waypoint) {
    map.removeLayer(waypoint);
    waypoints = waypoints.filter(marker => marker !== waypoint);
    waypoints.forEach((marker, index) => {
        marker._iconObject.update(index + 1);
    });
}

let stopWaypointsProcess = null;
let isWaypointStarted = false;
function startWaypoints() {
    isWaypointStarted = false;
    // find next waypoint
    let wp = waypoints.find(marker => marker._done == false);
    // if no more waypoints
    if (!wp) return;

    // send waypoint to drone
    let latlng = wp.getLatLng();
    function onTelemetryData(event) {
        let data = event.detail;

        // if 9 means started waypoint
        if (data.flight_mode == 9) {
            isWaypointStarted = true;
        }

        // when drone is in wait mode.
        if (isWaypointStarted && data.flight_mode == 3) {
            wp._done = true;
            wp._iconObject.update(null, 'done');
            document.removeEventListener('telemetry', onTelemetryData);
            setTimeout(startWaypoints, 0);
        }
    }

    // function to stop waypoints
    stopWaypointsProcess = function() {
        document.removeEventListener('telemetry', onTelemetryData);
        stopWaypointsProcess = null;
    }

    // when we reached waypoint
    document.addEventListener('telemetry', onTelemetryData);

    // send waypoint to drone
    sendTelemetryData(latlng.lat, latlng.lng);
}

function stopWaypoints() {
    // send current position to drone, to stop
    if (telemetryData && telemetryData.flight_mode != 3) {
        sendTelemetryData(telemetryData.lat, telemetryData.lon);
    }
}

function clearWaypoints() {
    stopWaypoints();
    waypoints.forEach((marker) => {
        map.removeLayer(marker);
    });
    waypoints = [];
}