from flask import Flask, send_file

app = Flask(__name__)

@app.route('/download/chunks/<filename>', methods=['GET'])
def download_chunk(filename):
    return send_file(f'chunks/{filename}', as_attachment=True)

@app.route('/download/list/<filename>', methods=['GET'])
def download_list(filename):
    return send_file(filename, as_attachment=True)

if __name__ == '__main__':
    app.run()