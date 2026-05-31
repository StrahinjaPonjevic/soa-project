<?php

namespace App\Services;

use RuntimeException;
use Throwable;

class TourCatalogException extends RuntimeException
{
    public function __construct(
        private readonly int $statusCode,
        string $message,
        ?Throwable $previous = null
    ) {
        parent::__construct($message, 0, $previous);
    }

    public function statusCode(): int
    {
        return $this->statusCode;
    }
}
