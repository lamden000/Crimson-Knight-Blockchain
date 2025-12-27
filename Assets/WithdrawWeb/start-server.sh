#!/bin/bash

echo "========================================"
echo "Starting HTTP Server for Withdraw Web"
echo "========================================"
echo ""
echo "Server will start at: http://localhost:8000"
echo "Press Ctrl+C to stop the server"
echo ""

cd "$(dirname "$0")"

# Try Python first
if command -v python3 &> /dev/null; then
    echo "Using Python3 HTTP Server..."
    python3 -m http.server 8000
    exit 0
fi

# Try Python 2
if command -v python &> /dev/null; then
    echo "Using Python HTTP Server..."
    python -m http.server 8000
    exit 0
fi

# Try Node.js http-server
if command -v npx &> /dev/null; then
    echo "Using Node.js http-server..."
    npx http-server -p 8000
    exit 0
fi

echo ""
echo "ERROR: No HTTP server found!"
echo ""
echo "Please install one of the following:"
echo "1. Python (https://www.python.org/)"
echo "2. Node.js (https://nodejs.org/) with npx"
echo ""
echo "Or manually start a server in this directory."
echo ""

