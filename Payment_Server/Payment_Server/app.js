const net = require('net');
const express = require('express')
const bodyParser = require('body-parser');
const fs = require('fs')
const logger = fs.createWriteStream('log.txt', { flags: 'a' })
const moment = require('moment')

function log(msg) {
	var output = moment().format("YYYY-MM-DD hh:mm:ss") + "," + msg + "\n"
	logger.write(output);
	console.log(output);
}

var events = {
	"1": {}
};
var callbacks = {
	"1": {}
}
function response_DS(work) {
	if (events[work.org_id][work.work_id] == undefined) { return }
	return new Promise((res) => {
		log("EXT," + JSON.stringify(work))
		callbacks[work.org_id][work.work_id](work.msg);
		delete events[work.org_id][work.work_id]
		delete callbacks[work.org_id][work.work_id]
		res()
    })
}



var work_id = 0;
var server = net.createServer(function (socket) {
	socket.on('data', function (data) {
		log("DS," + data)
		var json = JSON.parse(data)
		var wid = work_id++;
		json.work_id = wid
		events[json.org_id][wid] = json
		callbacks[json.org_id][wid] = function (msg) {
			socket.write(JSON.stringify(msg))
			socket.end();
		}
		setTimeout(() => {
			response_DS({
				org_id: json.org_id,
				work_id: wid,
				msg: { error: "Timeout" }
			})
		}, 10000)
	});
});
server.listen(1101, '0.0.0.0');



var app = express();
app.use(bodyParser.urlencoded({ extended: true }));
app.use(bodyParser.json());
app.get('/show_work', function (req, res) {
	res.send(JSON.stringify(events[req.query.org_id]))
});
app.post('/submit_work', function (req, res) {
	Promise.all(req.body.map((work) => {
		return response_DS(work)
	})).then(() => {
		res.send("OK")
    })
});
app.listen(5269, function () {
	log('Payment Server is now listening!');
});