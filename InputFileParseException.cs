using System;

namespace LPR381Solver.IO
{
    /// <summary>
    /// Thrown when an input file can't be tokenized/parsed into an LPModel -
    /// missing relation, wrong coefficient count, unrecognized sign restriction
    /// token, etc. Kept distinct from LPModel's own ModelValidationException so
    /// callers (and the menu's error handling) can tell "the file's syntax is
    /// wrong" apart from "the file parsed fine but describes an inconsistent
    /// model".
    /// </summary>
    public class InputFileParseException : Exception
    {
        public InputFileParseException(string message) : base(message) { }
    }
}
