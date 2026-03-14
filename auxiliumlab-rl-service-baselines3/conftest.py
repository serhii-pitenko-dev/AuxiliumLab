"""
Root-level conftest.py — loaded by pytest before any test collection.

The gRPC-generated stubs (generated/policy_trainer_pb2_grpc.py, etc.) use flat
imports such as `import policy_trainer_pb2`. These resolve only when the
generated/ directory itself is on sys.path.  server.py inserts that path at
startup, but pytest doesn't go through server.py, so we do it here instead.
"""
import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "generated"))
