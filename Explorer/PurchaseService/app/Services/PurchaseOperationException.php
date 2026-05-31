<?php

namespace App\Services;

use RuntimeException;

class PurchaseOperationException extends RuntimeException
{
    public function __construct(
        private readonly int $statusCode,
        string $message
    ) {
        parent::__construct($message);
    }

    public function statusCode(): int
    {
        return $this->statusCode;
    }
}
