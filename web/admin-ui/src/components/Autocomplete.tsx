import { useDebounce } from "@/hooks/useDebounce";
import { useEffect, useRef, useState, useTransition } from "react";
import type { FieldError } from "react-hook-form";

export interface AutocompleteOption {
  id: string;
  name: string;
}

export interface AutocompleteProps {
  value: string;
  onChange: (value: string) => void;
  onSearch: (term: string) => Promise<AutocompleteOption[]>;
  placeholder?: string;
  label?: string;
  error?: FieldError | string;
}

export default function Autocomplete({
  value,
  onChange,
  onSearch,
  placeholder = "Search...",
  label,
  error,
}: AutocompleteProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [results, setResults] = useState<AutocompleteOption[]>([]);
  const [searchTerm, setSearchTerm] = useState(value);
  const [selectedIndex, setSelectedIndex] = useState(-1);
  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const [isPending, startTransition] = useTransition();

  const debouncedSearch = useDebounce((term: string) => {
    startTransition(async () => {
      try {
        const data = await onSearch(term);
        setResults(data);
        setIsOpen(true);
        setSelectedIndex(-1);
      } catch (error) {
        console.error("Search failed:", error);
        setResults([]);
      }
    });
  }, 300);

  const handleSearch = (term: string) => {
    setSearchTerm(term);

    if (term.trim() === "") {
      setResults([]);
      setIsOpen(false);
      return;
    }

    debouncedSearch(term);
  };

  const handleSelectOption = (option: AutocompleteOption) => {
    onChange(option.id);
    setSearchTerm(option.name);
    setIsOpen(false);
    setResults([]);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!isOpen || results.length === 0) {
      if (e.key === "Enter") {
        e.preventDefault();
      }
      return;
    }

    switch (e.key) {
      case "ArrowDown":
        e.preventDefault();
        setSelectedIndex((prev) =>
          prev < results.length - 1 ? prev + 1 : prev
        );
        break;
      case "ArrowUp":
        e.preventDefault();
        setSelectedIndex((prev) => (prev > 0 ? prev - 1 : -1));
        break;
      case "Enter":
        e.preventDefault();
        if (selectedIndex >= 0) {
          handleSelectOption(results[selectedIndex]);
        }
        break;
      case "Escape":
        e.preventDefault();
        setIsOpen(false);
        break;
      default:
        break;
    }
  };

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (
        containerRef.current &&
        !containerRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  // Scroll selected item into view
  useEffect(() => {
    if (selectedIndex >= 0 && containerRef.current) {
      const items = containerRef.current.querySelectorAll(
        "[data-autocomplete-item]"
      );
      if (items[selectedIndex]) {
        items[selectedIndex].scrollIntoView({ block: "nearest" });
      }
    }
  }, [selectedIndex]);

  return (
    <div ref={containerRef} className="autocomplete-container">
      {label && <label className="autocomplete-label">{label}</label>}
      <div className="autocomplete-input-wrapper">
        <input
          ref={inputRef}
          type="text"
          value={searchTerm}
          onChange={(e) => handleSearch(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={() => searchTerm && setIsOpen(true)}
          placeholder={placeholder}
          className={`autocomplete-input ${error ? "error" : ""}`}
          autoComplete="off"
        />
        {isPending && <span className="autocomplete-loader">Loading...</span>}
      </div>

      {error && (
        <span className="autocomplete-error">
          {typeof error === "string" ? error : error.message}
        </span>
      )}

      {isOpen && results.length > 0 && (
        <ul className="autocomplete-results">
          {results.map((option, index) => (
            <li
              key={option.id}
              data-autocomplete-item
              className={`autocomplete-item ${
                index === selectedIndex ? "selected" : ""
              }`}
              onClick={() => handleSelectOption(option)}
            >
              {option.name}
            </li>
          ))}
        </ul>
      )}

      {isOpen && searchTerm && results.length === 0 && !isPending && (
        <div className="autocomplete-no-results">No results found</div>
      )}
    </div>
  );
}
