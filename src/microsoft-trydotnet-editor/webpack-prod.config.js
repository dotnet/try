const path = require('path');
const HtmlWebPackPlugin = require('html-webpack-plugin');

module.exports = {
	mode: 'production',
	output: {
		globalObject: 'self',
		filename: '[name].bundle.js',
		path: path.resolve(__dirname, '..', 'Microsoft.TryDotNet', 'wwwroot', 'api', 'editor')
	}
};