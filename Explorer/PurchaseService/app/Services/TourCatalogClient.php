<?php

namespace App\Services;

use Illuminate\Http\Client\Factory as HttpFactory;
use Illuminate\Http\Client\RequestException;

class TourCatalogClient
{
    public function __construct(private readonly HttpFactory $http)
    {
    }

    /**
     * @return array{id:int,name:string,price:float,status:string}
     */
    public function getTourSnapshot(int $tourId, string $authorization): array
    {
        $baseUrl = rtrim((string) config('services.tour_service.base_url'), '/');

        $response = $this->http
            ->withHeaders(['Authorization' => $authorization])
            ->acceptJson()
            ->get($baseUrl.'/api/tours/'.$tourId);

        if ($response->status() === 404) {
            throw new TourCatalogException(404, 'Tour not found.');
        }

        try {
            $response->throw();
        } catch (RequestException $exception) {
            throw new TourCatalogException(502, 'Tour service is unavailable.', $exception);
        }

        $payload = $response->json();
        if (!is_array($payload)) {
            throw new TourCatalogException(502, 'Invalid response from tour service.');
        }

        $name = $payload['name'] ?? null;
        $price = $payload['price'] ?? null;
        $status = $payload['status'] ?? null;

        if (!is_string($name) || (!is_int($price) && !is_float($price)) || !is_string($status)) {
            throw new TourCatalogException(502, 'Tour response missing required fields.');
        }

        return [
            'id' => $tourId,
            'name' => $name,
            'price' => (float) $price,
            'status' => $status,
        ];
    }
}
