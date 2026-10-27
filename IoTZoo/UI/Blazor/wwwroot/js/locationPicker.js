window.locationPicker = {
    maps: {},
    create: function (elementId, latitude, longitude, dotNetRef) {
        const map = new maplibregl.Map({
            container: elementId,
            style: "https://tiles.openfreemap.org/styles/liberty",
            center: [longitude, latitude],
            zoom: 9
        });
        map.addControl(new maplibregl.NavigationControl(), "top-right");
        const marker = new maplibregl.Marker({ color: "red" }).setLngLat([longitude, latitude]).addTo(map);
        map.on("click", function (e) {
            marker.setLngLat(e.lngLat);
            dotNetRef.invokeMethodAsync("SetLocation", e.lngLat.lat, e.lngLat.lng);
        });
        this.maps[elementId] = map;
    },
    drawRoute: function (elementId, wkt) {
        const map = this.maps[elementId];
        if (!map || !wkt) {
            return;
        }
        const body = wkt.substring(wkt.indexOf("(") + 1, wkt.lastIndexOf(")"));
        const coordinates = body.split(",").map(function (pair) {
            const [lat, lon] = pair.trim().split(/\s+/).map(Number);
            return [lon, lat];
        });
        const data = {
            type: "Feature",
            properties: {},
            geometry: { type: "LineString", coordinates: coordinates }
        };
        const draw = function () {
            const source = map.getSource("route");
            if (source) {
                source.setData(data);
            } else {
                map.addSource("route", { type: "geojson", data: data });
                map.addLayer({
                    id: "route",
                    type: "line",
                    source: "route",
                    layout: { "line-join": "round", "line-cap": "round" },
                    paint: { "line-color": "blue", "line-width": 5 }
                });
            }
            const bounds = coordinates.reduce(function (b, c) {
                return b.extend(c);
            }, new maplibregl.LngLatBounds(coordinates[0], coordinates[0]));
            map.fitBounds(bounds, { padding: 40 });
        };
        if (map.isStyleLoaded()) {
            draw();
        } else {
            map.once("load", draw);
        }
    },
    dispose: function (elementId) {
        const map = this.maps[elementId];
        if (map) {
            map.remove();
            delete this.maps[elementId];
        }
    }
};
