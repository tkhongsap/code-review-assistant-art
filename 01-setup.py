import os

def get_programming_files(directory):
    """
    Recursively scan directory for programming files and return their paths
    """
    programming_extensions = {
        '.cs', '.cpp', '.c', '.h', '.hpp',  # C-family
        '.js', '.jsx', '.ts', '.tsx',       # JavaScript/TypeScript
        '.py', '.pyc',                      # Python
        '.java', '.kt',                     # Java/Kotlin
        '.rb',                              # Ruby
        '.php',                             # PHP
        '.go',                              # Go
        '.rs',                              # Rust
        '.swift',                           # Swift
        '.m', '.mm',                        # Objective-C
        '.scala',                           # Scala
        '.fs', '.fsx',                      # F#
        '.vb',                              # Visual Basic
        '.pl', '.pm',                       # Perl
        '.sh', '.bash',                     # Shell scripts
        '.sql',                             # SQL
        '.r',                               # R
        '.dart',                            # Dart
        '.lua',                             # Lua
        '.clj',                             # Clojure
        '.ex', '.exs'                       # Elixir
    }

    file_list = []
    
    for root, dirs, files in os.walk(directory):
        for file in files:
            # Get the file extension
            _, ext = os.path.splitext(file)
            
            # Check if it's a programming file
            if ext.lower() in programming_extensions:
                # Get relative path from input directory
                rel_path = os.path.relpath(os.path.join(root, file), directory)
                file_list.append(rel_path)
                
    return sorted(file_list)

def main():
    # Get the absolute path to the input directory
    current_dir = os.path.dirname(os.path.abspath(__file__))
    input_dir = os.path.join(current_dir, 'input')
    
    # Check if input directory exists
    if not os.path.exists(input_dir):
        print(f"Error: Input directory not found at {input_dir}")
        return
    
    # Get list of programming files
    files = get_programming_files(input_dir)
    
    # Create output file
    output_file = os.path.join(current_dir, 'files-structure.txt')
    
    # Write to output file
    with open(output_file, 'w', encoding='utf-8') as f:
        for file_path in files:
            f.write(f"{file_path}\n")
            
    print(f"Found {len(files)} programming files")
    print(f"File structure has been written to {output_file}")

if __name__ == "__main__":
    main()
