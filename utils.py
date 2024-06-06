# utils.py
import threading

# Define a lock for synchronizing access to shared data
users_lock = threading.Lock()
online_users = {}