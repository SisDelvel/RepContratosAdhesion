const sharePointService = {
    uploadFile: async (file, path) => {

        const fileConfiguration = {
            ACCESS_TOKEN: constantesSharePoint.ACCESS_TOKEN,
            DRIVE_ID: constantesSharePoint.DRIVE_ID,
            file,
        };

        if (!fileConfiguration.ACCESS_TOKEN || !fileConfiguration.DRIVE_ID || !fileConfiguration.file) {
            throw new Error(`No es posible subir el archivo debido a que faltan datos`);
        }

        const url = `https://graph.microsoft.com/v1.0/drives/${fileConfiguration.DRIVE_ID}/root:/${path}/${file.name}:/content`;

        const headers = new Headers({ "Authorization": `Bearer ${fileConfiguration.ACCESS_TOKEN}`, "Content-Type": fileConfiguration.file.type });

        const res = await request(url, { method: 'PUT', headers, body: fileConfiguration.file, stringifyBody: false });


        return {
            url: res["@microsoft.graph.downloadUrl"],
            uniqueId: new URL(res["@microsoft.graph.downloadUrl"]).searchParams.get("UniqueId"),
            tamano: fileConfiguration.file.size,
            nombre: fileConfiguration.file.name,
            mimeType: fileConfiguration.file.type
        }

    },
    generateAccessToken: async () => {
        const res = await fetch(`/Sharepoint/GenerarToken`);
        if (!res.ok) throw new Error(`Error al obtener token (${res.status})`);
        return await res.json();
    },
    deleteFile: async FILE_ID => await request(`https://graph.microsoft.com/v1.0/drives/${constantesSharePoint.DRIVE_ID}/items/${FILE_ID}`, { method: "DELETE", headers: new Headers({ "Authorization": `Bearer ${constantesSharePoint.ACCESS_TOKEN}` }) }),
    getFilePublicUrl: async FILE_ID => await request(`https://graph.microsoft.com/v1.0/drives/${constantesSharePoint.DRIVE_ID}/items/${FILE_ID}`, { method: "GET", headers: new Headers({ "Authorization": `Bearer ${constantesSharePoint.ACCESS_TOKEN}` }) }),
    getFilesPublicUrls: async FILE_IDS => {
        const responses = await Promise.all(
            FILE_IDS.map(id =>
                request(`https://graph.microsoft.com/v1.0/drives/${constantesSharePoint.DRIVE_ID}/items/${id}`, {
                    method: "GET",
                    headers: new Headers({
                        "Authorization": `Bearer ${constantesSharePoint.ACCESS_TOKEN}`
                    })
                })
            )
        );

        return responses.map(r => ({
            uniqueId: new URL(r["@microsoft.graph.downloadUrl"]).searchParams.get("UniqueId"),
            url: r["@microsoft.graph.downloadUrl"],
        }));
    }
};