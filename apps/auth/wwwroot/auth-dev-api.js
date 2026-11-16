window.authDevApi = {
    call: async function (method, url, body) {
        const response = await fetch(url, {
            method: method,
            credentials: "same-origin",
            headers: body === null || body === undefined
                ? {}
                : { "Content-Type": "application/json" },
            body: body === null || body === undefined
                ? undefined
                : JSON.stringify(body)
        });

        return {
            statusCode: response.status,
            status: response.status + " " + response.statusText,
            body: await response.text()
        };
    },

    callForm: async function (url, values) {
        const response = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: { "Content-Type": "application/x-www-form-urlencoded" },
            body: new URLSearchParams(values)
        });

        return {
            statusCode: response.status,
            status: response.status + " " + response.statusText,
            body: await response.text()
        };
    }
};
