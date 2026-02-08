function initRouteMap() {
    const mapElement = document.getElementById('routeMap');
    if (!mapElement) return;

    if (!googleMapsKey) {
        mapElement.innerHTML = '<div style="padding:16px; color:#475569;">Google Maps API key is missing.</div>';
        return;
    }

    const defaultCenter = { lat: 1.3521, lng: 103.8198 };
    const firstStop = stops.find(s => s.lat && s.lng);
    const map = new google.maps.Map(mapElement, {
        center: firstStop ? { lat: firstStop.lat, lng: firstStop.lng } : defaultCenter,
        zoom: 12,
        mapTypeControl: false,
        streetViewControl: false,
        fullscreenControl: false,
        zoomControl: false
    });

    const pendingColor = '#f97316';
    const doneColor = '#22c55e';
    const activeColor = '#2563eb';

    const bounds = new google.maps.LatLngBounds();
    const routePath = [];
    const markersById = new Map();
    const infoById = new Map();
    const stopsById = new Map(stops.map(stop => [String(stop.id), stop]));
    let activeMarkerId = null;
    let directionsService = null;
    let directionsRenderer = null;
    let routeDirectionsService = null;
    let routeDirectionsRenderer = null;
    let simplePolyline = null;
    let currentLocation = null;
    let hasGpsOptimized = false;

    stops.forEach((stop) => {
        if (!stop.lat || !stop.lng) return;

        const color = stop.status === 'completed' ? doneColor : pendingColor;
        const marker = new google.maps.Marker({
            position: { lat: stop.lat, lng: stop.lng },
            map,
            label: {
                text: String(stop.seq),
                color: '#ffffff',
                fontWeight: '700'
            },
            icon: {
                path: google.maps.SymbolPath.CIRCLE,
                scale: 14,
                fillColor: color,
                fillOpacity: 1,
                strokeColor: '#ffffff',
                strokeWeight: 4
            }
        });

        const info = new google.maps.InfoWindow({
            content: `<strong>Stop ${stop.seq}</strong><br>${stop.location}<br>${stop.address}`
        });

        marker.addListener('click', () => {
            const stopId = String(stop.id);
            setActiveStop(stopId);
            info.open({ anchor: marker, map });
            showDirections(stopId);
        });

        markersById.set(String(stop.id), marker);
        infoById.set(String(stop.id), info);

        bounds.extend(marker.getPosition());
        routePath.push(marker.getPosition());
    });

    const pendingStops = stops.filter(stop => stop.status === 'pending' && stop.lat && stop.lng);

    const drawSimplePolyline = () => {
        if (routePath.length === 0) return;
        if (!simplePolyline) {
            simplePolyline = new google.maps.Polyline({
                strokeColor: '#93c5fd',
                strokeOpacity: 1,
                strokeWeight: 4
            });
            simplePolyline.setMap(map);
        }
        simplePolyline.setPath(routePath);
        map.fitBounds(bounds, 40);
    };

    const renderOptimizedRoute = (originOverride) => {
        const routeStops = pendingStops.length > 1
            ? pendingStops
            : stops.filter(stop => stop.lat && stop.lng);

        if (routeStops.length < 2) {
            drawSimplePolyline();
            return;
        }

        if (!routeDirectionsService) {
            routeDirectionsService = new google.maps.DirectionsService();
        }
        if (!routeDirectionsRenderer) {
            routeDirectionsRenderer = new google.maps.DirectionsRenderer({
                map,
                suppressMarkers: true,
                polylineOptions: {
                    strokeColor: '#3b82f6',
                    strokeOpacity: 0.85,
                    strokeWeight: 5
                }
            });
        }

        const origin = originOverride
            ? { lat: originOverride.lat, lng: originOverride.lng }
            : { lat: routeStops[0].lat, lng: routeStops[0].lng };
        const destination = { lat: routeStops[routeStops.length - 1].lat, lng: routeStops[routeStops.length - 1].lng };
        const waypointsSource = originOverride
            ? routeStops.slice(0, -1)
            : routeStops.slice(1, -1);
        const waypoints = waypointsSource.map(stop => ({
            location: { lat: stop.lat, lng: stop.lng },
            stopover: true
        }));

        routeDirectionsService.route(
            {
                origin,
                destination,
                waypoints,
                optimizeWaypoints: true,
                travelMode: google.maps.TravelMode.DRIVING
            },
            (result, status) => {
                if (status !== 'OK' || !result) {
                    drawSimplePolyline();
                    return;
                }
                routeDirectionsRenderer.setDirections(result);
                map.fitBounds(result.routes[0].bounds, 40);
            }
        );
    };

    if (routePath.length > 0) {
        renderOptimizedRoute();
    }

    const nextEl = document.querySelector('.map-next');
    if (nextEl && nextAddress) {
        nextEl.textContent = `Next: ${nextAddress}`;
    }

    const toRad = (value) => (value * Math.PI) / 180;
    const haversine = (a, b) => {
        const earthRadius = 6371000;
        const dLat = toRad(b[0] - a[0]);
        const dLng = toRad(b[1] - a[1]);
        const lat1 = toRad(a[0]);
        const lat2 = toRad(b[0]);
        const h = Math.sin(dLat / 2) ** 2 + Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLng / 2) ** 2;
        return 2 * earthRadius * Math.asin(Math.sqrt(h));
    };

    const updateNextBanner = (currentLatLng) => {
        if (!nextEl || pendingStops.length === 0) return;
        const distances = pendingStops.map((stop, index) => ({
            stop,
            index,
            distance: haversine(currentLatLng, [stop.lat, stop.lng])
        }));

        distances.sort((a, b) => a.index - b.index);
        const nearby = distances.find(item => item.distance <= 50);
        if (nearby) {
            const nextIndex = nearby.index + 1;
            const nextStop = pendingStops[nextIndex] ?? nearby.stop;
            nextEl.textContent = `Next: ${nextStop.address || nextStop.location}`;
        }
    };

    const getMarkerIcon = (color, scale = 14) => ({
        path: google.maps.SymbolPath.CIRCLE,
        scale,
        fillColor: color,
        fillOpacity: 1,
        strokeColor: '#ffffff',
        strokeWeight: 4
    });

    const updateMarkerIcon = (marker, stop, isActive) => {
        if (!marker) return;
        const color = isActive ? activeColor : (stop.status === 'completed' ? doneColor : pendingColor);
        const scale = isActive ? 16 : 14;
        marker.setIcon(getMarkerIcon(color, scale));
    };

    const setActiveStop = (stopId) => {
        const stop = stopsById.get(String(stopId));
        if (!stop) return;

        document.querySelectorAll('.stop-card').forEach(card => card.classList.remove('is-active'));
        const card = document.querySelector(`.stop-card[data-stop-id="${stopId}"]`);
        if (card) card.classList.add('is-active');

        if (nextEl) {
            nextEl.textContent = `Selected: ${stop.address || stop.location}`;
        }

        const marker = markersById.get(String(stopId));
        if (marker) {
            updateMarkerIcon(marker, stop, true);
            if (activeMarkerId && activeMarkerId !== String(stopId)) {
                const prevStop = stopsById.get(activeMarkerId);
                const prevMarker = markersById.get(activeMarkerId);
                if (prevMarker && prevStop) {
                    updateMarkerIcon(prevMarker, prevStop, false);
                }
            }
            activeMarkerId = String(stopId);
            map.panTo(marker.getPosition());
            map.setZoom(Math.max(map.getZoom(), 14));

            const info = infoById.get(String(stopId));
            if (info) info.open({ anchor: marker, map });
        }
    };

    const showDirections = (stopId) => {
        const stop = stopsById.get(String(stopId));
        if (!stop || !stop.lat || !stop.lng) return;
        if (!currentLocation) {
            if (nextEl) nextEl.textContent = 'Enable location to get directions.';
            return;
        }

        if (!directionsService) {
            directionsService = new google.maps.DirectionsService();
        }
        if (!directionsRenderer) {
            directionsRenderer = new google.maps.DirectionsRenderer({
                map,
                suppressMarkers: true,
                polylineOptions: {
                    strokeColor: '#3b82f6',
                    strokeOpacity: 0.9,
                    strokeWeight: 5
                }
            });
        }

        directionsService.route(
            {
                origin: currentLocation,
                destination: { lat: stop.lat, lng: stop.lng },
                travelMode: google.maps.TravelMode.DRIVING
            },
            (result, status) => {
                if (status !== 'OK' || !result) {
                    if (nextEl) nextEl.textContent = 'Unable to get directions.';
                    return;
                }
                directionsRenderer.setDirections(result);
                const leg = result.routes[0].legs[0];
                if (nextEl && leg?.distance?.text) {
                    nextEl.textContent = `Route: ${leg.distance.text} • ${stop.address || stop.location}`;
                }
                map.fitBounds(result.routes[0].bounds, 40);
            }
        );
    };

    const startNavigation = (stopId) => {
        const stop = stopsById.get(String(stopId));
        if (!stop || !stop.lat || !stop.lng) return;
        if (!currentLocation) {
            if (nextEl) nextEl.textContent = 'Enable location to start navigation.';
            return;
        }

        const origin = `${currentLocation.lat},${currentLocation.lng}`;
        const destination = `${stop.lat},${stop.lng}`;
        const url = `https://www.google.com/maps/dir/?api=1&origin=${encodeURIComponent(origin)}&destination=${encodeURIComponent(destination)}&travelmode=driving`;
        window.open(url, '_blank', 'noopener');
    };

    let myMarker = null;
    if (navigator.geolocation) {
        navigator.geolocation.watchPosition(
            (pos) => {
                const me = { lat: pos.coords.latitude, lng: pos.coords.longitude };
                currentLocation = me;
                if (!hasGpsOptimized && routePath.length > 0) {
                    hasGpsOptimized = true;
                    renderOptimizedRoute(me);
                }
                if (!myMarker) {
                    myMarker = new google.maps.Marker({
                        position: me,
                        map,
                        icon: {
                            path: google.maps.SymbolPath.CIRCLE,
                            scale: 8,
                            fillColor: '#3b82f6',
                            fillOpacity: 0.9,
                            strokeColor: '#3b82f6',
                            strokeWeight: 2
                        },
                        title: 'Your Location'
                    });
                } else {
                    myMarker.setPosition(me);
                }
                updateNextBanner([me.lat, me.lng]);
            },
            (err) => console.log('Geolocation error:', err.message),
            { enableHighAccuracy: true, maximumAge: 5000, timeout: 10000 }
        );
    }

    const zoomInButton = document.getElementById('zoomIn');
    const zoomOutButton = document.getElementById('zoomOut');

    if (zoomInButton && zoomOutButton) {
        zoomInButton.addEventListener('click', () => map.setZoom(map.getZoom() + 1));
        zoomOutButton.addEventListener('click', () => map.setZoom(map.getZoom() - 1));
    }

    document.querySelectorAll('.stop-card[data-stop-id]').forEach((card) => {
        card.addEventListener('click', (event) => {
            if (event.target.closest('.direction-trigger')) return;
            const stopId = card.dataset.stopId;
            if (stopId) {
                setActiveStop(stopId);
                showDirections(stopId);
            }
        });
    });

    document.querySelectorAll('.direction-trigger').forEach((button) => {
        button.addEventListener('click', (event) => {
            event.stopPropagation();
            const stopId = button.dataset.stopId;
            if (stopId) {
                setActiveStop(stopId);
                showDirections(stopId);
            }
        });
    });

    document.querySelectorAll('.start-nav-trigger').forEach((button) => {
        button.addEventListener('click', (event) => {
            event.stopPropagation();
            const stopId = button.dataset.stopId;
            if (stopId) {
                setActiveStop(stopId);
                startNavigation(stopId);
            }
        });
    });
}

const collectModal = document.getElementById('collectModal');
const collectModalClose = document.getElementById('collectModalClose');
const collectCancel = document.getElementById('collectCancel');
const collectSubtitle = document.getElementById('collectModalSubtitle');
const collectForm = document.getElementById('collectForm');
const collectFeedback = document.getElementById('collectFeedback');
const collectStopId = document.getElementById('collectStopId');
const collectPointId = document.getElementById('collectPointId');
const collectLocationName = document.getElementById('collectLocationName');
const collectAddress = document.getElementById('collectAddress');
const collectBinId = document.getElementById('collectBinId');
const collectFillRange = document.getElementById('collectFillRange');
const collectFillValue = document.getElementById('collectFillValue');

const openCollectModal = (data) => {
    if (!collectModal) return;
    collectStopId.value = data.stopId || '';
    collectPointId.value = data.binLabel || '';
    collectLocationName.value = data.locationName || '';
    collectAddress.value = data.address || '';
    collectBinId.value = data.binId || '';
    collectFillRange.value = data.fillLevel || 0;
    collectFillValue.textContent = `${collectFillRange.value}%`;
    collectSubtitle.textContent = `Record collection details for ${data.locationName}`;
    collectModal.classList.add('is-open');
    collectModal.setAttribute('aria-hidden', 'false');
};

const closeCollectModal = () => {
    if (!collectModal) return;
    collectModal.classList.remove('is-open');
    collectModal.setAttribute('aria-hidden', 'true');
};

document.querySelectorAll('.collect-trigger').forEach((button) => {
    button.addEventListener('click', () => {
        openCollectModal({
            stopId: button.dataset.stopId,
            binId: button.dataset.binId,
            binLabel: button.dataset.binId ? `B${String(button.dataset.binId).padStart(3, '0')}` : '',
            locationName: button.dataset.locationName,
            address: button.dataset.address,
            fillLevel: button.dataset.fillLevel
        });
    });
});

if (collectFillRange && collectFillValue) {
    collectFillRange.addEventListener('input', () => {
        collectFillValue.textContent = `${collectFillRange.value}%`;
    });
}

// Support for standalone ConfirmCollection page
const fillLevelRange = document.getElementById('fillLevelRange');
const fillLevelValue = document.getElementById('fillLevelValue');
if (fillLevelRange && fillLevelValue) {
    fillLevelValue.textContent = `${fillLevelRange.value}%`;
    fillLevelRange.addEventListener('input', () => {
        fillLevelValue.textContent = `${fillLevelRange.value}%`;
    });
}

if (collectModal) {
    collectModal.addEventListener('click', (event) => {
        if (event.target === collectModal) {
            closeCollectModal();
        }
    });
}

if (collectModalClose) {
    collectModalClose.addEventListener('click', closeCollectModal);
}

if (collectCancel) {
    collectCancel.addEventListener('click', closeCollectModal);
}

const updateStatusCounts = () => {
    const pendingCards = document.querySelectorAll('.stop-card.pending');
    const completeCards = document.querySelectorAll('.stop-card.complete');
    const totalCards = pendingCards.length + completeCards.length;
    const percent = totalCards > 0 ? Math.round((completeCards.length / totalCards) * 100) : 0;

    const pendingStatus = document.querySelector('.details-status.pending');
    const completeStatus = document.querySelector('.details-status.complete');
    const updateCount = (element, count) => {
        if (element) {
            element.textContent = element.textContent.replace(/\(\d+\)/, `(${count})`);
        }
    };

    updateCount(pendingStatus, pendingCards.length);
    updateCount(completeStatus, completeCards.length);

    const progressValue = document.getElementById('collectionProgressValue');
    if (progressValue) progressValue.textContent = `${completeCards.length}/${totalCards}`;

    const progressPending = document.getElementById('collectionProgressPending');
    if (progressPending) progressPending.textContent = `Pending: ${pendingCards.length}`;

    const progressPercent = document.getElementById('progressPercent');
    if (progressPercent) progressPercent.textContent = `${percent}% Complete`;

    const progressFill = document.getElementById('progressFill');
    if (progressFill) progressFill.style.width = `${percent}%`;
};

const handleCollectionUIUpdate = (result) => {
    const stopId = String(result.stopId || '');
    const completedTime = result.collectedAt || '';
    const card = document.querySelector(`.stop-card[data-stop-id="${stopId}"]`);

    if (card) {
        card.classList.remove('pending');
        card.classList.add('complete');

        const badge = card.querySelector('.stop-badge');
        if (badge) {
            badge.classList.remove('pending');
            badge.classList.add('complete');
        }

        const meta = card.querySelector('.stop-meta');
        if (meta) meta.textContent = `Completed: ${completedTime || '-'}`;

        const actions = card.querySelector('.stop-actions');
        if (actions) actions.innerHTML = '<i class="bi bi-check-circle-fill text-success"></i>';

        const completeStatus = document.querySelector('.details-status.complete');
        const completeSection = completeStatus ? completeStatus.closest('.details-section') : null;
        if (completeSection) completeSection.appendChild(card);
    }
    updateStatusCounts();
};

const parseErrorResponse = async (response) => {
    let message = 'Unable to complete collection.';
    const contentType = response.headers.get('content-type') || '';
    if (contentType.includes('application/json')) {
        const payload = await response.json();
        if (payload && payload.errors) {
            const firstKey = Object.keys(payload.errors)[0];
            const firstError = firstKey ? payload.errors[firstKey][0] : '';
            if (firstError) message = firstError;
        }
    }
    return message;
};

if (collectForm) {
    collectForm.addEventListener('submit', async (event) => {
        event.preventDefault();
        if (collectFeedback) collectFeedback.textContent = '';

        try {
            const response = await fetch(collectForm.action, {
                method: 'POST',
                body: new FormData(collectForm),
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'Accept': 'application/json'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                const message = await parseErrorResponse(response);
                if (collectFeedback) collectFeedback.textContent = message;
                return;
            }

            const contentType = response.headers.get('content-type') || '';
            if (!contentType.includes('application/json')) {
                if (collectFeedback) collectFeedback.textContent = 'Session expired. Please refresh and try again.';
                return;
            }

            const result = await response.json();
            if (result && result.success) {
                handleCollectionUIUpdate(result);
                closeCollectModal();
            } else if (collectFeedback) {
                collectFeedback.textContent = 'Unable to complete collection.';
            }
        } catch (error) {
            if (collectFeedback) collectFeedback.textContent = 'Network error. Please try again.';
        }
    });
}

// Support for ReportIssue View
const binSelect = document.getElementById('binSelect');
if (binSelect) {
    binSelect.addEventListener('change', function () {
        const selectedOption = this.options[this.selectedIndex];
        const locationDetails = document.getElementById('locationDetails');

        if (this.value) {
            const location = selectedOption.dataset.location;
            const region = selectedOption.dataset.region;

            const displayLocation = document.getElementById('displayLocation');
            const displayRegion = document.getElementById('displayRegion');

            if (displayLocation) displayLocation.textContent = location || '';
            if (displayRegion) displayRegion.textContent = region || '';
            if (locationDetails) locationDetails.style.display = 'block';
        } else {
            if (locationDetails) locationDetails.style.display = 'none';
        }
    });
}

// Support for MyRouteAssignments View
const drawerElement = document.getElementById('routeDetailDrawer');
const drawer = drawerElement ? new bootstrap.Offcanvas(drawerElement) : null;

if (drawerElement && drawer) {
    const statusBadge = document.getElementById('drawerRouteStatus');
    const titleEl = document.getElementById('drawerRouteTitle');
    const routeIdEl = document.getElementById('drawerRouteId');
    const dateEl = document.getElementById('drawerPlannedDate');
    const regionEl = document.getElementById('drawerRegion');
    const assignedToEl = document.getElementById('drawerAssignedTo');
    const assignedByEl = document.getElementById('drawerAssignedBy');
    const progressTextEl = document.getElementById('drawerProgressText');
    const progressBarEl = document.getElementById('drawerProgressBar');
    const detailsLink = document.getElementById('drawerDetailsLink');

    document.querySelectorAll('.route-detail-trigger').forEach((trigger) => {
        trigger.addEventListener('click', () => {
            const routeId = trigger.dataset.routeId || '-';
            const status = trigger.dataset.routeStatus || 'Pending';
            const plannedDate = trigger.dataset.plannedDate || '-';
            const region = trigger.dataset.region || '-';
            const assignedTo = trigger.dataset.assignedTo || '-';
            const assignedBy = trigger.dataset.assignedBy || '-';
            const totalStops = trigger.dataset.totalStops || '0';
            const completedStops = trigger.dataset.completedStops || '0';
            const progress = trigger.dataset.progress || '0';
            const assignmentId = trigger.dataset.assignmentId;

            if (titleEl) titleEl.textContent = `Route #${routeId}`;
            if (routeIdEl) routeIdEl.textContent = routeId;
            if (dateEl) dateEl.textContent = plannedDate;
            if (regionEl) regionEl.textContent = region;
            if (assignedToEl) assignedToEl.textContent = assignedTo;
            if (assignedByEl) assignedByEl.textContent = assignedBy;
            if (progressTextEl) progressTextEl.textContent = `${completedStops}/${totalStops} bins completed`;
            if (progressBarEl) progressBarEl.style.width = `${progress}%`;

            if (statusBadge) {
                statusBadge.textContent = status;
                statusBadge.className = 'badge mb-3';
                if (status === 'Completed') {
                    statusBadge.classList.add('bg-success');
                } else if (status === 'Active' || status === 'In Progress') {
                    statusBadge.classList.add('bg-primary');
                } else {
                    statusBadge.classList.add('bg-warning', 'text-dark');
                }
            }

            if (assignmentId && detailsLink) {
                detailsLink.href = `/CollectorDashboard/RouteAssignmentDetails/${assignmentId}`;
            }

            drawer.show();
        });
    });
}
