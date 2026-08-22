window.userInfo = {
    getDeviceInfo: function () {
        return navigator.userAgent;
    },
    getLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Geolocation not supported.");
            }

            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                (error) => {
                    reject("Error getting location.");
                }
            );
        });
    }
};
