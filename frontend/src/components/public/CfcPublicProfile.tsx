import React from 'react';
import { CfcPublicProfile } from '@/lib/api/public';
import { MapPin, Phone, Mail, Building, Car } from 'lucide-react';
import { Badge } from '@/components/ui/badge';

interface CfcPublicProfileProps {
  profile: CfcPublicProfile;
}

export function CfcPublicProfileComponent({ profile }: CfcPublicProfileProps) {
  return (
    <div className="max-w-3xl mx-auto p-6 bg-white shadow-md rounded-lg mt-10">
      <div className="flex items-center space-x-4 mb-6 border-b pb-4">
        <Building className="w-12 h-12 text-primary" />
        <div>
          <h1 className="text-3xl font-bold text-gray-900">{profile.tradingName}</h1>
          <p className="text-gray-500">{profile.name} - CNPJ: {profile.cnpj}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <h2 className="text-xl font-semibold mb-3 flex items-center">
            <MapPin className="w-5 h-5 mr-2" /> Endereço
          </h2>
          <p className="text-gray-700">{profile.address.street}, {profile.address.number}</p>
          {profile.address.complement && <p className="text-gray-700">{profile.address.complement}</p>}
          <p className="text-gray-700">{profile.address.neighborhood}</p>
          <p className="text-gray-700">{profile.address.city} - {profile.address.state}</p>
          <p className="text-gray-700">CEP: {profile.address.zipCode}</p>
        </div>

        <div>
          <h2 className="text-xl font-semibold mb-3 flex items-center">
            <Phone className="w-5 h-5 mr-2" /> Contato
          </h2>
          <p className="text-gray-700 flex items-center">
             {profile.contact.phone}
          </p>
          <p className="text-gray-700 flex items-center mt-2">
            <Mail className="w-4 h-4 mr-2" /> {profile.contact.email}
          </p>
        </div>
      </div>

      <div className="mt-8">
        <h2 className="text-xl font-semibold mb-3 flex items-center">
          <Car className="w-5 h-5 mr-2" /> Categorias Disponíveis
        </h2>
        <div className="flex flex-wrap gap-2">
          {profile.categories.map((cat) => (
            <Badge key={cat} variant="secondary" className="text-lg py-1 px-3">
              Categoria {cat}
            </Badge>
          ))}
        </div>
      </div>
    </div>
  );
}
