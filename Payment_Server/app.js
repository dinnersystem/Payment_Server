const net = require('net');
const express = require('express')
const bodyParser = require('body-parser');
const fs = require('fs')
const logger = fs.createWriteStream('log.txt', { flags: 'a' })
const moment = require('moment')
const config = require('./config')
const https = require('https');

var events = config.events
var callbacks = config.callbacks

function response_DS(work, org_id) {
	return new Promise((res) => {
		if (events[org_id][work.work_id] != undefined) {
			log("EXT_RESP," + JSON.stringify(work))
			callbacks[org_id][work.work_id](work.msg);
			events[org_id][work.work_id] = undefined
			callbacks[org_id][work.work_id] = undefined
		}
		res()
	})
}
function log(msg) {
	var output = moment().format("YYYY-MM-DD hh:mm:ss") + "," + msg + "\n"
	logger.write(output);
	console.log(output);
}

var work_id = 0;
var server = net.createServer(function (socket) {
	socket.on('data', function (data) {
		log("DS_REQ_START," + data)
		var json = JSON.parse(data)
		var wid = work_id++;
		json.work_id = wid
		events[json.org_id][wid] = json
		callbacks[json.org_id][wid] = function (msg) {
			msg = JSON.stringify(msg)
			log("DS_RESP_START," + msg)
			socket.write(msg)
			socket.end();
			log("DS_RESP_END," + msg)
		}
		setTimeout(() => {
			response_DS({
				org_id: json.org_id,
				work_id: wid,
				msg: { error: "Timeout" }
			}, json.org_id)
		}, config.network.EXT_timeout)
		log("DS_REQ_END," + JSON.stringify(json))
	});
});
server.listen(config.network.DS_port, '127.0.0.1');

var app = express();
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());
app.post('/show_work', function (req, res) {
	log(`i get ${req.headers.authorization}`)
	log(`i want ${config.auth}`)
	log(`i get orgid ${config.auth[req.headers.authorization]}`)
	var org_id = config.auth[req.headers.authorization]
	if (org_id == undefined) return;
	log("EXT_REQ," + org_id)
	res.send(JSON.stringify(events[org_id]))
});
app.post('/submit_work', function (req, res) {
	var org_id = config.auth[req.headers.authorization]
	if (org_id == undefined) return;
	Promise.all(req.body.payload.map((work) => {
		return response_DS(work, org_id)
	})).then(() => {
		log("EXT_RESP_END,")
		res.send("OK")
	}).catch((err) => {
		log("Error," + JSON.stringify(err))
		res.send("Error")
    })
});
if (config.network.https.enable) {
	https.createServer({
		key: fs.readFileSync(config.network.https.key, 'utf8'),
		cert: fs.readFileSync(config.network.https.cert, 'utf8')
	}, app).listen(config.network.EXT_port, function () { log('Payment Server is now listening(with https)!'); });
} else {
	app.listen(config.network.EXT_port, function () { log('Payment Server is now listening(with http)!'); });
}
