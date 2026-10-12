window.locationPicker = {
    maps: {},
    create: function (elementId, latitude, longitude, dotNetRef) {
        const map = L.map(elementId).setView([latitude, longitude], 9);
        L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
            maxZoom: 19,
            attribution: "&copy; OpenStreetMap contributors"
        }).addTo(map);
        const marker = L.marker([latitude, longitude]).addTo(map);
        map.on("click", function (e) {
            marker.setLatLng(e.latlng);
            dotNetRef.invokeMethodAsync("SetLocation", e.latlng.lat, e.latlng.lng);
        });
        this.maps[elementId] = map;
    },
    dispose: function (elementId) {
        const map = this.maps[elementId];
        if (map) {
            map.remove();
            delete this.maps[elementId];
        }
    }
};
