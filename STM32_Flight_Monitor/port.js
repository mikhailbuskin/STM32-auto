const { SerialPort } = require('serialport')

module.exports.listPorts = function () {
  SerialPort.list().then(ports => {
    console.log('== Available Ports:');
    ports.forEach(port => {
        console.log(`${port.path} Manufacturer: ${port.manufacturer}`);
    });
  }).catch(err => {
      console.error('Error listing ports:', err);
  });
  return;
}

module.exports.findPort = async function (manufacturer) {
  var ports = await SerialPort.list();
  return ports.find(port => (port.manufacturer + '').trim() == manufacturer);
}