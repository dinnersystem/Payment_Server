module.exports = {
	events: {
		"1": {}
	},
	callbacks: {
		"1": {}
	},
	auth: {
		"Bearer 878787": "1"
	},
	network: {
		EXT_port: 5269,
		DS_port: 1101,
		DS_timeout: 30000,
		https: {
			enable: false,
			key: '../Resource/server.key',
			cert: '../Resource/server.crt'
        }
    }
}