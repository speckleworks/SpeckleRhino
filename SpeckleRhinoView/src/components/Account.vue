<template>
  <v-list-group :value='true'>
    <v-list-tile slot='item' @click=''>
      <v-list-tile-content>
        <v-list-tile-title v-html="account.serverName"></v-list-tile-title>
        <v-list-tile-sub-title v-html="account.email"></v-list-tile-sub-title>
      </v-list-tile-content>
      <v-list-tile-action>
        <v-icon>keyboard_arrow_down</v-icon>
      </v-list-tile-action>
    </v-list-tile>
    <v-list-tile slot='sub'>
      <v-list-tile-content>
        <v-list-tile-title>{{account.restApi}}</v-list-tile-title>
        <v-list-tile-sub-title>{{account.apiToken}}</v-list-tile-sub-title>
      </v-list-tile-content>
    </v-list-tile>
  </v-list-group>
</template>

<script>
export default {
  name: 'Account',
  props: {
    account: Object
  },
  data() {
    return {
      active: true
    }
  },
  methods: {
    deleteAccount() {
      Interop.removeAccount( this.account.fileName )
      this.$store.commit( 'DELETE_ACCOUNT', this.account.fileName )
    }
  }
}
</script>

<style lang="scss">
.account {
  margin-bottom: 10px;
}
</style>