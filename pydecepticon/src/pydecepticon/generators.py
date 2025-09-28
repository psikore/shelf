import locale
from enum import Enum
import random
from typing import Optional


class NameGeneratorMethod(Enum):
    NONE = 0
    RANDOM_COMBINATIONS = 1
    AVOID_TWIN_CHARACTERS = 2
    DICTIONARY_WORDS = 3
    MARKOV = 4
    SUFFIX = 5


class NameGenerator:
    def __init__(
            self,
            generation_method: NameGeneratorMethod,
            character_set: Optional[str] = None,
            dictionary_words: Optional[list] = None,
            markov_generator = None,
            min_length: Optional[int] = 2,
            max_length: Optional[int] = 16,
    ):
        self.generation_method = generation_method
        self.character_set = character_set
        self.dictionary_words = dictionary_words
        self.markov_generator = markov_generator
        self.counter = 0
        self.min_length = min_length
        self.max_length = max_length

    def generate(self, name=None) -> str:
        if self.generation_method == NameGeneratorMethod.RANDOM_COMBINATIONS:
            return self.generate_random_string()
        elif self.generation_method == NameGeneratorMethod.AVOID_TWIN_CHARACTERS:
            return self.generate_no_twin_string()
        elif self.generation_method == NameGeneratorMethod.DICTIONARY_WORDS:
            return self.generate_dictionary_string()
        elif self.generation_method == NameGeneratorMethod.MARKOV:
            return self.generate_markov_words()
        elif self.generation_method == NameGeneratorMethod.SUFFIX:
            return self.generate_suffix_string(original_name=name)
        return ""

    def generate_suffix_string(self, original_name: str) -> str:
        new_name = original_name + "_" + str(self.counter)
        self.counter += 1
        return new_name

    def generate_random_string(self, length=0):
        l = length if length > 0 else random.randint(self.min_length, self.max_length)
        return ''.join(random.choice(self.character_set) for _ in range(l))

    def generate_no_twin_string(self, length=0, attempt_limit=100):
        l = length if length > 0 else random.randint(self.min_length, self.max_length)
        if not self.character_set:
            return ""

        value = [random.choice(self.character_set)]
        for i in range(1, l):
            attempt_count = 0
            while attempt_count < attempt_limit:
                c = random.choice(self.character_set)
                if c != value[i - 1]:
                    value.append(c)
                    break
                attempt_count += 1
            else:
                return ""
        return ''.join(value)

    def generate_dictionary_string(self, length=0):
        l = length if length > 0 else random.randint(self.min_length, self.max_length)
        return ''.join(random.choice(self.dictionary_words) for _ in range(l))

    def generate_markov_words(
            self,
            min_words=2,
            max_words=8,
            min_length=3,
            max_length=8,
    ):
        def default_markov_generator(
                _count,
                min_len,
                max_len,
        ):
            syllables = ["ka", "zu", "mi", "ra", "lo", "ta", "ne", "shi", "vo", "da"]
            _words = []
            for _ in range(_count):
                word_len = random.randint(min_len, max_len)
                word = ''.join(random.choices(syllables, k=word_len // 2))
                _words.append(word)
            return _words

        generator = self.markov_generator or default_markov_generator
        count = random.randint(min_words, max_words)
        words = generator(count, min_length, max_length)

        # locale aware title casing
        locale.setlocale(locale.LC_ALL, '')
        title_cased = [w.lower().capitalize() for w in words]
        return ''.join(title_cased)
