import os
import sys
from dotenv import load_dotenv
from utils.openai_utils import mask_string
from utils.terminal_utils import print_info, print_error

# Add the project root to the Python path
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

# Load environment variables
load_dotenv()

openai_api_key = os.getenv("OPENAI_API_KEY")
openai_assistant_id = os.getenv("OPENAI_ASSISTANT_ID")

# Print masked versions of sensitive data
print_info(f"Using assistant ID: {mask_string(openai_assistant_id)}")
print_info(f"Using API key: {mask_string(openai_api_key)}")

