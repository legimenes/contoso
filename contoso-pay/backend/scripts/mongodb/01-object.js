db = db.getSiblingDB('Contoso');

db.createUser({
  user: 'appuser',
  pwd: 'pass@word',
  roles: [
    {
      role: 'readWrite',
      db: 'Contoso',
    },
  ],
});

db.createCollection('ContosoPay');