<?php

namespace App\Http\Requests;

use Illuminate\Foundation\Http\FormRequest;

class AddToCartRequest extends FormRequest
{
    public function authorize(): bool
    {
        return true;
    }

    public function rules(): array
    {
        return [
            'tourId'     => ['required', 'integer', 'min:1'],
            'tourName'   => ['required', 'string', 'max:255'],
            'tourPrice'  => ['required', 'numeric', 'min:0'],
            'tourStatus' => ['required', 'string'],
        ];
    }
}
