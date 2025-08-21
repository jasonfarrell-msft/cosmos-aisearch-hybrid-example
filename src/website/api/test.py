
import os
from send_query import answer_generator
from dotenv import load_dotenv

load_dotenv()

response = answer_generator.answer_query("networking")
print(response)