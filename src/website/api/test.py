
import os
from send_query import answer_generator
from dotenv import load_dotenv

load_dotenv()

response = answer_generator.answer_query("how do you feel about networking?")
print(response)