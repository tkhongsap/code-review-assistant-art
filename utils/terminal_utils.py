from enum import Enum
from typing import Optional

class TerminalColors:
    HEADER = '\033[95m'
    BLUE = '\033[94m'
    CYAN = '\033[96m'
    GREEN = '\033[92m'
    WARNING = '\033[93m'
    FAIL = '\033[91m'
    ENDC = '\033[0m'
    BOLD = '\033[1m'
    UNDERLINE = '\033[4m'

def color_text(text: str, color: str) -> str:
    """Add color to terminal text"""
    return f"{color}{text}{TerminalColors.ENDC}"

def print_header(text: str):
    """Print header text in purple"""
    print(color_text(text, TerminalColors.HEADER))

def print_success(text: str):
    """Print success message in green"""
    print(color_text(text, TerminalColors.GREEN))

def print_info(text: str):
    """Print info message in cyan"""
    print(color_text(text, TerminalColors.CYAN))

def print_warning(text: str):
    """Print warning message in yellow"""
    print(color_text(text, TerminalColors.WARNING))

def print_error(text: str):
    """Print error message in red"""
    print(color_text(text, TerminalColors.FAIL)) 