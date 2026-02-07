// Jest setup file for polyfills and global test configuration

// Polyfill for TextEncoder/TextDecoder (required by jsdom)
const { TextEncoder, TextDecoder } = require('util');

global.TextEncoder = TextEncoder;
global.TextDecoder = TextDecoder;
